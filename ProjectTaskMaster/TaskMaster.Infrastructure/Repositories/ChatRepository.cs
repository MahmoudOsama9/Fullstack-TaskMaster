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
    public class ChatRepository : IChatRepository
    {
        private readonly TaskMasterDbContext _context;

        public ChatRepository(TaskMasterDbContext context)
        {
            _context = context;
        }

        public async Task<ChatMessage> AddMessageAsync(ChatMessage message)
        {
            _context.ChatMessages.Add(message);
            await _context.SaveChangesAsync();
            return message;
        }

        public async Task<IEnumerable<ChatMessage>> GetMessagesByProjectIdAsync(int projectId)
        {
            return await _context.ChatMessages
            .Where(m => m.ProjectId == projectId)
            .OrderBy(m => m.CreatedAt)
            .Include(m => m.Sender)
            .ToListAsync();
        }
    }
}
