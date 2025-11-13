using HotChocolate.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using TaskMaster.API.DTOs;
using TaskMaster.API.Hubs;
using TaskMaster.Infrastructure.Data;

namespace TaskMaster.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class TasksController : Controller
    {
        private readonly ILogger<TasksController> _logger;
        private readonly IHubContext<ProjectUpdatesHub> _hubContext;
        private readonly TaskMasterDbContext _context;
        public TasksController(ILogger<TasksController> logger, IHubContext<ProjectUpdatesHub> hubContext, TaskMasterDbContext context)
        {
            _logger = logger;
            _hubContext = hubContext;
            _context = context;
        }
        [HttpPut("{taskId}/status")]
        public async Task<IActionResult> UpdateTaskStatus(int taskId, [FromBody] UpdateStatusDto updateStatusDto)
        {
            var task = await _context.TaskItems.FindAsync(taskId);

            if (task == null)
            {
                return NotFound();
            }

            task.Status = updateStatusDto.Status;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Task {TaskId} status updated to {Status}", taskId, updateStatusDto.Status);

            await _hubContext.Clients.Group(task.ProjectId.ToString())
                .SendAsync("TaskStatusUpdated", new
                {
                    TaskId = taskId,
                    NewStatus = updateStatusDto.Status
                });

            return Ok(new { Message = "Status updated and notification sent." });
        }
    }
}