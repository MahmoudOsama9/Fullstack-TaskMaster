using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using TaskMaster.Core.Entities;
using TaskMaster.Core.Interfaces;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;


namespace TaskMaster.Infrastructure.Repositories
{
    public class CachedProjectRepository : IProjectRepository
    {
        private readonly IProjectRepository _decorated;
        private readonly StackExchange.Redis.IDatabase _redisDatabase;
        private readonly ILogger<CachedProjectRepository> _logger;

        public CachedProjectRepository(IProjectRepository decorate, StackExchange.Redis.IConnectionMultiplexer redisConnection, ILogger<CachedProjectRepository> logger)
        {
            _decorated = decorate;
            _redisDatabase = redisConnection.GetDatabase();
            _logger = logger;
        }

        public async Task<Project> AddProjectAsync(Project project)
        {
            return await _decorated.AddProjectAsync(project);
        }

        public async Task<bool> DeleteProjectAsync(int projectId)
        {
            var success = await _decorated.DeleteProjectAsync(projectId);
            if (success)
            {
                string cacheKey = $"project:{projectId}";
                await _redisDatabase.KeyDeleteAsync(cacheKey);
                _logger.LogInformation("Cache INVALIDATED for key: {CacheKey}", cacheKey);
            }
            return success;
        }

        public async Task<IEnumerable<Project>> GetProjectsAsync(int userId)
        {
            return await _decorated.GetProjectsAsync(userId);
        }

        public async Task<Project?> GetProjectByIdAsync(int id)
        {
            string cacheKey = $"project:{id}";
            var cachedProject = await _redisDatabase.StringGetAsync(cacheKey);

            if (!cachedProject.IsNullOrEmpty)
            {
                _logger.LogInformation("Cache HIT for key: {CacheKey}", cacheKey);
                return JsonSerializer.Deserialize<Project>(cachedProject!);
            }

            _logger.LogInformation("Cache MISS for key: {CacheKey}", cacheKey);

            var project = await _decorated.GetProjectByIdAsync(id);

            if (project != null)
            {
                var serializedProject = JsonSerializer.Serialize(project);
                await _redisDatabase.StringSetAsync(cacheKey, serializedProject, TimeSpan.FromMinutes(5));
            }

            return project;
        }

        public async Task<Project?> UpdateProjectAsync(Project project)
        {
            var updatedProject = await _decorated.UpdateProjectAsync(project);
            if (updatedProject != null)
            {
                string cacheKey = $"project:{project.Id}";
                await _redisDatabase.KeyDeleteAsync(cacheKey);
                _logger.LogInformation("Cache INVALIDATED for key: {CacheKey}", cacheKey);
            }
            return updatedProject;
        }
    }
}
