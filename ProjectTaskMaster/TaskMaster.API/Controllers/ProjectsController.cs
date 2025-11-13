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

namespace TaskMaster.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ProjectsController : ControllerBase
    {
        private readonly IProjectRepository _projectRepository;
        private readonly IHubContext<ProjectUpdatesHub> _hubContext;

        public ProjectsController(IProjectRepository projectRepository, IHubContext<ProjectUpdatesHub> hubContext)
        {
            _projectRepository = projectRepository;
            _hubContext = hubContext;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ProjectDto>>> GetProjects()
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdString, out var userId))
            {
                return Unauthorized();
            }

            var projects = await _projectRepository.GetProjectsAsync(userId);

            var projectDtos = new List<ProjectDto>();
            if (projects != null)
            {
                foreach (var p in projects)
                {
                    var dto = new ProjectDto(
                        p.Id,
                        p.Name,
                        p.Description,
                        p.CreatedAt,
                        p.DueDate,
                        p.Status,
                        p.Tasks?.Count ?? 0
                    );
                    projectDtos.Add(dto);
                }
            }

            return Ok(projectDtos);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ProjectDto>> GetProject(int id)
        {
            var project = await _projectRepository.GetProjectByIdAsync(id);
            if (project == null)
            {
                return NotFound();
            }
            var projectDTO = new ProjectDto(project.Id, project.Name, project.Description, project.CreatedAt, project.DueDate, project.Status, project.Tasks.Count);
            return Ok(projectDTO);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<ProjectDto>> UpdateProject(int id, UpdateProjectDto updateDto)
        {
            // 1. Fetch the existing project from the database.
            var projectToUpdate = await _projectRepository.GetProjectByIdAsync(id);

            if (projectToUpdate == null)
            {
                return NotFound($"Project with ID {id} not found.");
            }

            // 2. Authorization Check: Verify the current user owns the project.
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (projectToUpdate.OwnerId.ToString() != userIdString)
            {
                return Forbid();
            }

            // 3. Map changes from the DTO to the entity.
            projectToUpdate.Name = updateDto.Name;
            projectToUpdate.Description = updateDto.Description!;
            projectToUpdate.DueDate = updateDto.DueDate;
            projectToUpdate.Status = updateDto.Status;

            // 4. Save the changes to the database.
            var updatedProject = await _projectRepository.UpdateProjectAsync(projectToUpdate);

            if (updatedProject == null)
            {
                return StatusCode(500, "An error occurred while updating the project.");
            }

            // 5. Map the final, updated entity to a DTO.
            var projectDto = new ProjectDto(
                updatedProject.Id,
                updatedProject.Name,
                updatedProject.Description,
                updatedProject.CreatedAt,
                updatedProject.DueDate,
                updatedProject.Status,
                updatedProject.Tasks.Count
            );

            // --- NEW SIGNALR BROADCAST LOGIC ---

            // 6. Send a targeted message with the full updated project data
            // to any client currently in the SignalR group for this specific project.
            await _hubContext.Clients.All.SendAsync("ProjectListShouldRefresh");
            await _hubContext.Clients.Group(updatedProject.Id.ToString())
                .SendAsync("ProjectDetailsUpdated", projectDto);

            // 7. Send a general message to ALL connected clients, telling them
            // that the project list is now stale and should be refreshed.
            await _hubContext.Clients.All.SendAsync("ProjectListShouldRefresh");

            // --- END OF SIGNALR LOGIC ---

            // 8. Return a 200 OK response with the updated project data to the original caller.
            return Ok(projectDto);
        }


        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProject(int id)
        {
            var project = await _projectRepository.GetProjectByIdAsync(id);
            if (project == null) return NotFound();
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (project.OwnerId.ToString() != userIdString)
            {
                return Forbid();
            }

            await _projectRepository.DeleteProjectAsync(id);

            await _hubContext.Clients.All.SendAsync("ProjectListShouldRefresh");


            return NoContent();
        }

        [HttpPost]
        public async Task<ActionResult<ProjectDto>> CreateProject(CreateProjectDto createProjectDTO)
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdString, out var userId))
            {
                return Unauthorized();
            }

            var newProject = new Project
            {
                Name = createProjectDTO.Name,
                Description = createProjectDTO.Description!,
                DueDate = createProjectDTO.DueDate,
                OwnerId = userId
            };

            var createdProject = await _projectRepository.AddProjectAsync(newProject);

            var createdProjectDTO = new ProjectDto(createdProject.Id, createdProject.Name, createdProject.Description, createdProject.CreatedAt, createdProject.DueDate, createdProject.Status, 0);

            await _hubContext.Clients.All.SendAsync("ProjectListShouldRefresh");

            return CreatedAtAction(nameof(GetProjects), new { id = createdProjectDTO.Id }, createdProjectDTO);
        }
        //[HttpPut("tasks/{taskId}/status")]
        //public async Task<IActionResult> UpdateTaskStatus(int taskId, [FromBody] string newStatus)
        //{
            
        //}
    }
}
