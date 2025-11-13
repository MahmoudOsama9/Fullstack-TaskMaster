using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskMaster.Core.Entities;

namespace TaskMaster.Core.Interfaces
{
    public interface IProjectRepository
    {
        Task<Project?> GetProjectByIdAsync(int projectId);
        Task<IEnumerable<Project>> GetProjectsAsync(int userId);
        Task<Project> AddProjectAsync(Project project);
        Task<Project?> UpdateProjectAsync(Project project);
        Task<bool> DeleteProjectAsync(int projectId);
    }
}
