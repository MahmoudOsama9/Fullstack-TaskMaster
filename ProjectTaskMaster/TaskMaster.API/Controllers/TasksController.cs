using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using TaskMaster.API.Hubs;
using TaskMaster.Core.Entities;
using TaskMaster.Core.Enums;
using TaskMaster.Core.Interfaces;
using TaskMaster.Infrastructure.Data;
using TaskMaster.API.DTOs;

namespace TaskMaster.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class TasksController : ControllerBase
{
    private readonly ITaskRepository _taskRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly IHubContext<ProjectUpdatesHub> _hubContext;
    private readonly TaskMasterDbContext _context;

    public TasksController(
        ITaskRepository taskRepository,
        IProjectRepository projectRepository,
        IHubContext<ProjectUpdatesHub> hubContext,
        TaskMasterDbContext context)
    {
        _taskRepository = taskRepository;
        _projectRepository = projectRepository;
        _hubContext = hubContext;
        _context = context;
    }

    [HttpGet("project/{projectId}")]
    public async Task<ActionResult<IEnumerable<TaskDto>>> GetTasks(int projectId)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        var project = await _projectRepository.GetProjectByIdAsync(projectId);
        if (project == null) return NotFound("Project not found.");

        bool isOwner = project.OwnerId == userId;
        bool isMember = project.Memberships.Any(m => m.UserId == userId);

        if (!isOwner && !isMember) return Forbid();

        var tasks = await _taskRepository.GetTasksByProjectIdAsync(projectId);

        var taskDtos = tasks.Select(t => new TaskDto(
            t.Id,
            t.Title,
            t.Description,
            t.Status,
            t.Priority.ToString(),
            t.DueDate,
            t.AssignedToUserId,
            t.AssignedToUser?.Name
        ));

        return Ok(taskDtos);
    }

    [HttpPost("project/{projectId}")]
    public async Task<IActionResult> CreateTask(int projectId, CreateTaskDto dto)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var project = await _projectRepository.GetProjectByIdAsync(projectId);

        if (project == null) return NotFound("Project not found.");

        var membership = project.Memberships.FirstOrDefault(m => m.UserId == userId);
        bool canEdit = project.OwnerId == userId || (membership != null && membership.Role != ProjectRole.Viewer);

        if (!canEdit) return Forbid();

        Enum.TryParse(dto.Priority, true, out PriorityLevel priority);

        var task = new TaskItem
        {
            ProjectId = projectId,
            Title = dto.Title,
            Description = dto.Description,
            DueDate = dto.DueDate,
            Status = Stage.NotStarted.ToString(),
            Priority = (int)priority,
            AssignedToUserId = dto.AssignedToUserId,
            CreatedAt = DateTime.UtcNow
        };

        await _taskRepository.AddAsync(task);

        await _hubContext.Clients.Group(projectId.ToString()).SendAsync("TaskCreated", task);

        return Ok(task);
    }

    [HttpPut("{taskId}/status")]
    public async Task<IActionResult> UpdateTaskStatus(int taskId, [FromBody] UpdateTaskStatusDto dto)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var task = await _taskRepository.GetByIdAsync(taskId);

        if (task == null) return NotFound();

        var project = await _projectRepository.GetProjectByIdAsync(task.ProjectId);
        if (project == null) return NotFound();

        bool isMember = project.OwnerId == userId || project.Memberships.Any(m => m.UserId == userId);
        if (!isMember) return Forbid();

        task.Status = dto.Status;
        await _taskRepository.UpdateAsync(task);

        await _hubContext.Clients.Group(project.Id.ToString()).SendAsync("TaskStatusUpdated", new { TaskId = task.Id, NewStatus = task.Status });

        return Ok(new { Message = "Status updated." });
    }

    [HttpDelete("{taskId}")]
    public async Task<IActionResult> DeleteTask(int taskId)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var task = await _taskRepository.GetByIdAsync(taskId);
        if (task == null) return NotFound();

        var project = await _projectRepository.GetProjectByIdAsync(task.ProjectId);

        bool isOwner = project!.OwnerId == userId;
        bool isAdmin = project.Memberships.Any(m => m.UserId == userId && m.Role == ProjectRole.Admin);

        if (!isOwner && !isAdmin) return Forbid();

        await _taskRepository.DeleteAsync(taskId);

        await _hubContext.Clients.Group(project.Id.ToString()).SendAsync("TaskDeleted", taskId);

        return NoContent();
    }

    [HttpGet("{taskId}/notes")]
    public async Task<ActionResult<IEnumerable<NoteDto>>> GetNotes(int taskId)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        var task = await _taskRepository.GetByIdAsync(taskId);
        if (task == null) return NotFound();

        var project = await _projectRepository.GetProjectByIdAsync(task.ProjectId);
        bool isMember = project.OwnerId == userId || project.Memberships.Any(m => m.UserId == userId);
        if (!isMember) return Forbid();

        var notes = await _taskRepository.GetNotesAsync(taskId);

        return Ok(notes.Select(n => new NoteDto(
            n.Id,
            n.Content,
            n.User?.Name ?? "Unknown",
            n.CreatedAt
        )));
    }

    [HttpPost("{taskId}/notes")]
    public async Task<IActionResult> AddNote(int taskId, [FromBody] CreateNoteDto dto)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var userName = User.Identity?.Name ?? "Unknown";

        var task = await _taskRepository.GetByIdAsync(taskId);
        if (task == null) return NotFound();

        var project = await _projectRepository.GetProjectByIdAsync(task.ProjectId);

        var membership = project.Memberships.FirstOrDefault(m => m.UserId == userId);
        bool canEdit = project.OwnerId == userId || (membership != null && membership.Role != ProjectRole.Viewer);

        if (!canEdit) return Forbid();

        var note = new TaskNote
        {
            TaskId = taskId,
            AutherId = userId,
            Content = dto.Content,
            CreatedAt = DateTime.UtcNow
        };

        await _taskRepository.AddNoteAsync(note);

        var noteDto = new NoteDto(note.Id, note.Content, userName, note.CreatedAt);

        await _hubContext.Clients.Group(project.Id.ToString()).SendAsync("NoteAdded", new { TaskId = taskId, Note = noteDto });

        return Ok(noteDto);
    }
}