using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic;
using Polly;
using System.Security.Claims;
using TaskMaster.API.DTOs;
using TaskMaster.API.Hubs;
using TaskMaster.Core.Entities;
using TaskMaster.Core.Interfaces;
using TaskMaster.Infrastructure.Data;

namespace TaskMaster.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ProjectsController : ControllerBase
    {
        private readonly IProjectRepository _projectRepository;
        private readonly IHubContext<ProjectUpdatesHub> _hubContext;
        private readonly ILogger<ProjectsController> _logger;
        private readonly TaskMasterDbContext _context;

        public ProjectsController(IProjectRepository projectRepository, IHubContext<ProjectUpdatesHub> hubContext,
            ILogger<ProjectsController> logger, TaskMasterDbContext context)
        {
            _logger = logger;
            _context = context;
            _projectRepository = projectRepository;
            _hubContext = hubContext;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ProjectDto>>> GetProjects()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);

            var projects = await _projectRepository.GetProjectsAsync(userId);

            var projectDTOs = projects.Select(p => MapToDto(p, userId)).ToList();
            return Ok(projectDTOs);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ProjectDto>> GetProject(int id)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            var project = await _projectRepository.GetProjectByIdAsync(id);

            if (project == null) return NotFound();

            bool isOwner = project.OwnerId == userId;
            bool isMember = project.Memberships.Any(m => m.UserId == userId);

            if (!isOwner && !isMember)
            {
                return Forbid();
            }

            return Ok(MapToDto(project, userId));
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<ProjectDto>> UpdateProject(int id, UpdateProjectDto updateDto)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            var project = await _projectRepository.GetProjectByIdAsync(id);
            if (project == null) return NotFound();

            var membership = project.Memberships.FirstOrDefault(m => m.UserId == userId);
            bool isOwner = project.OwnerId == userId;
            bool isAdmin = membership?.Role == Core.Enums.ProjectRole.Admin;

            if (!isOwner && !isAdmin) return Forbid();

            project.Name = updateDto.Name;
            project.Description = updateDto.Description!;
            project.DueDate = updateDto.DueDate;
            project.Status = updateDto.Status;

            var updatedProject = await _projectRepository.UpdateProjectAsync(project);
            if (updatedProject == null) return StatusCode(500, "Error updating project.");

            var dto = MapToDto(updatedProject, userId);

            await _hubContext.Clients.Group(id.ToString()).SendAsync("ProjectDetailsUpdated", dto);

            return Ok(dto);
        }


        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProject(int id)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            var project = await _projectRepository.GetProjectByIdAsync(id);
            if (project == null) return NotFound();

            if (project.OwnerId != userId) return Forbid();

            await _projectRepository.DeleteProjectAsync(id);

            await _hubContext.Clients.Group(id.ToString()).SendAsync("ProjectDeleted", id);

            return NoContent();
        }

        [HttpPost]
        public async Task<ActionResult<ProjectDto>> CreateProject(CreateProjectDto createProjectDto)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            var newProject = new Project
            {
                Name = createProjectDto.Name,
                Description = createProjectDto.Description!,
                DueDate = createProjectDto.DueDate,
                OwnerId = userId,
                Status = "NotStarted",
                CreatedAt = DateTime.UtcNow
            };

            var createdProject = await _projectRepository.AddProjectAsync(newProject);

            await _hubContext.Clients.User(userId.ToString()).SendAsync("ProjectListShouldRefresh");

            var membersDto = new List<ProjectMemberDto>
            {
                new ProjectMemberDto(createdProject.OwnerId, User.Identity?.Name ?? "Owner", "Owner")
            };

            var dto = new ProjectDto(
                createdProject.Id, 
                createdProject.Name, 
                createdProject.Description,
                createdProject.CreatedAt, 
                createdProject.DueDate, 
                createdProject.Status,
                0, 
                0, 
                0, 
                "Owner", 
                membersDto,
                false
            );

            return CreatedAtAction(nameof(GetProject), new { id = createdProject.Id }, dto);
        }

        private ProjectDto MapToDto(Project p, int userId)
        {
            var totalTasks = p.Tasks?.Count ?? 0;
            var completedTasks = p.Tasks?.Count(t => t.Status == "Completed") ?? 0;
            var progress = totalTasks == 0 ? 0 : (int)((double)completedTasks / totalTasks * 100);

            string role = "Viewer";
            if (p.OwnerId == userId) role = "Owner";
            else
            {
                var membership = p.Memberships?.FirstOrDefault(m => m.UserId == userId);
                if (membership != null) role = membership.Role.ToString();
            }

            var membersDto = new List<ProjectMemberDto>();

            if (p.Owner != null)
            {
                membersDto.Add(new ProjectMemberDto(p.OwnerId, p.Owner.Name, "Owner"));
            }

            if (p.Memberships != null)
            {
                foreach (var m in p.Memberships)
                {
                    var name = m.User?.Name ?? "Unknown";
                    membersDto.Add(new ProjectMemberDto(m.UserId, name, m.Role.ToString()));
                }
            }

            return new ProjectDto(
                p.Id, 
                p.Name, 
                p.Description, 
                p.CreatedAt, 
                p.DueDate, 
                p.Status,
                totalTasks, 
                completedTasks, 
                progress, 
                role,
                membersDto,
                false
            );
        }
    }
}
