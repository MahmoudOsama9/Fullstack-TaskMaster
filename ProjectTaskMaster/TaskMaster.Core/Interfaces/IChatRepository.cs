using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskMaster.Core.Entities;

namespace TaskMaster.Core.Interfaces
{
    public interface IChatRepository
    {
        Task<IEnumerable<ChatMessage>> GetMessagesByProjectIdAsync(int projectId);
        Task<ChatMessage> AddMessageAsync(ChatMessage message);
    }
}
