using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskMaster.Core.Entities;
using TaskMaster.Core.Interfaces;
using TaskMaster.Infrastructure.Data;

namespace TaskMaster.Infrastructure.Repositories
{
    public class ProjectRepository : IProjectRepository
    {
        private readonly TaskMasterDbContext _context;

        public ProjectRepository(TaskMasterDbContext context) { 
            _context = context;
        }

        public async Task<Project> AddProjectAsync(Project project)
        {
            await _context.Projects.AddAsync(project);

            await _context.SaveChangesAsync();
            return project;
        }

        public async Task<bool> DeleteProjectAsync(int projectId)
        {
            var projectToDelete = await _context.Projects.FindAsync(projectId);
            if (projectToDelete == null) return false;

            _context.Projects.Remove(projectToDelete);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<Project>> GetProjectsAsync(int userId)
        {
            return await _context.Projects
            .AsNoTracking()
            .Include(p => p.Tasks)
            .Where(p =>
                p.OwnerId == userId
                ||
                p.Teams.Any(t => t.Members.Any(m => m.Id == userId))
            )
            .ToListAsync();
        }


        public async Task<Project?> GetProjectByIdAsync(int projectId)
        {
            var project = await _context.Projects.FindAsync(projectId);

            if(project == null)
            {
                return null;
            }

            return await _context.Projects
            .AsNoTracking()
            .Include(p => p.Tasks)
            .FirstOrDefaultAsync(p => p.Id == projectId);
        }

        public async Task<Project?> UpdateProjectAsync(Project project)
        {
            var existingProject = await _context.Projects.FindAsync(project.Id);
            if (existingProject == null) return null;
            _context.Entry(existingProject).CurrentValues.SetValues(project);
            await _context.SaveChangesAsync();
            return existingProject;
        }
    }
}
