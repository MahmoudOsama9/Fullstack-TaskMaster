using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskMaster.Core.Entities;

namespace TaskMaster.Core.Interfaces
{
    public interface IInvitationRepository
    {
        Task<ProjectInvitation?> GetByIdAsync(int id);
        Task<IEnumerable<ProjectInvitation>> GetPendingInvitationsAsync(string userEmail);
        Task<ProjectInvitation> AddAsync(ProjectInvitation invitation);
        Task UpdateAsync(ProjectInvitation invitation);
        Task<bool> HasPendingInviteAsync(int projectId, string email);
    }
}
