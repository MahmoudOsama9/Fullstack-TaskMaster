using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using TaskMaster.API.Protos;
using TaskMaster.Core.Enums;
using TaskMaster.Infrastructure.Data;
using Microsoft.Extensions.Logging;


namespace TaskMaster.API.Services
{
    public class ProjectReporterService : ProjectReporter.ProjectReporterBase
    {
        private readonly TaskMasterDbContext _dbContext;
        private readonly ILogger<ProjectReporterService> _logger;

        public ProjectReporterService(TaskMasterDbContext dbContext, ILogger<ProjectReporterService> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public override async Task<ProjectSummaryResponse> GetProjectSummary(ProjectSummaryRequest request, ServerCallContext context)
        {
            _logger.LogInformation("gRPC request for project summary received for projectId: {ProjectId}", request.ProjectId);

            var project = await _dbContext.Projects
                .Include(p => p.Tasks)
                .FirstOrDefaultAsync(p => p.Id == request.ProjectId);

            if (project == null)
            {
                throw new RpcException(new Status(StatusCode.NotFound, $"Project with ID {request.ProjectId} not found."));
            }

            var completedStatus = Stage.Completed.ToString();
            var completedTasks = project.Tasks.Count(t => t.Status == completedStatus);
            

            return new ProjectSummaryResponse
            {
                ProjectId = project.Id,
                ProjectName = project.Name,
                TotalTasks = project.Tasks.Count,
                CompletedTasks = completedTasks,
                PendingTasks = project.Tasks.Count(t => t.Status != completedStatus),
                
            };
        }
    }
}
