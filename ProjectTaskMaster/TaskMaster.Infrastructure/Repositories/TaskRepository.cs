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
    public class TaskRepository : ITaskRepository
    {
        private readonly TaskMasterDbContext _context;

        public TaskRepository(TaskMasterDbContext context)
        {
            _context = context;
        }

        public async Task<TaskItem?> GetByIdAsync(int id)
        {
            return await _context.TaskItems.FindAsync(id);
        }

        public async Task<TaskItem> AddAsync(TaskItem task)
        {
            _context.TaskItems.Add(task);
            await _context.SaveChangesAsync();
            return task;
        }

        public async Task<IEnumerable<TaskItem>> GetTasksByProjectIdAsync(int projectId)
        {
            return await _context.TaskItems
                .Include(t => t.AssignedToUser)
                .Where(t => t.ProjectId == projectId)
                .ToListAsync();
        }
        public async Task UpdateAsync(TaskItem task)
        {
            _context.Entry(task).State = EntityState.Modified;
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var task = await _context.TaskItems.FindAsync(id);
            if (task != null)
            {
                _context.TaskItems.Remove(task);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<IEnumerable<TaskNote>> GetNotesAsync(int taskId)
        {
            return await _context.TaskNotes
                .Where(n => n.TaskId == taskId)
                .Include(n => n.User)
                .OrderBy(n => n.CreatedAt)
                .ToListAsync();
        }

        public async Task<TaskNote> AddNoteAsync(TaskNote note)
        {
            _context.TaskNotes.Add(note);
            await _context.SaveChangesAsync();
            return note;
        }
    }
}
