using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskMaster.Core.Entities;
using TaskMaster.Core.Enums;
using TaskMaster.Core.Interfaces;
using TaskMaster.Infrastructure.Data;

namespace TaskMaster.Infrastructure.Repositories
{
    public class InvitationRepository : IInvitationRepository
    {
        private readonly TaskMasterDbContext _context;

        public InvitationRepository(TaskMasterDbContext context)
        {
            _context = context;
        }

        public async Task<ProjectInvitation?> GetByIdAsync(int id)
        {
            return await _context.ProjectInvitations
                .Include(i => i.Project)
                .ThenInclude(p => p.Memberships)
                .FirstOrDefaultAsync(i => i.Id == id);
        }

        public async Task<IEnumerable<ProjectInvitation>> GetPendingInvitationsAsync(string userEmail)
        {
            return await _context.ProjectInvitations
                .Include(i => i.Project)
                .Include(i => i.Inviter)
                .Where(i => i.InviterEmail == userEmail && i.Status == InvitationStatus.Pending)
                .OrderByDescending(i => i.CreatedAt)
                .ToListAsync();
        }

        public async Task<ProjectInvitation> AddAsync(ProjectInvitation invitation)
        {
            _context.ProjectInvitations.Add(invitation);
            await _context.SaveChangesAsync();
            return invitation;
        }

        public async Task UpdateAsync(ProjectInvitation invitation)
        {
            _context.Entry(invitation).State = EntityState.Modified;
            await _context.SaveChangesAsync();
        }

        public async Task<bool> HasPendingInviteAsync(int projectId, string email)
        {
            return await _context.ProjectInvitations
                .AnyAsync(i => i.ProjectId == projectId && i.InviterEmail == email && i.Status == InvitationStatus.Pending);
        }
    }
}
