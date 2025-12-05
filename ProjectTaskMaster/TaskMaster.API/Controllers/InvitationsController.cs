using HotChocolate.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TaskMaster.API.Hubs;
using TaskMaster.Core.Entities;
using TaskMaster.Core.Enums;
using TaskMaster.Infrastructure.Data;

namespace TaskMaster.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class InvitationsController : Controller
    {
        private readonly TaskMasterDbContext _context;
        private readonly IHubContext<ProjectUpdatesHub> _hubContext;

        public InvitationsController(TaskMasterDbContext context, IHubContext<ProjectUpdatesHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        public record InviteRequestDto(string Email, ProjectRole Role);

        [HttpPost("project/{projectId}")]
        public async Task<IActionResult> InviteMember(int projectId, [FromBody] InviteRequestDto request)
        {
            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            var project = await _context.Projects
                .Include(p => p.Memberships)
                .FirstOrDefaultAsync(p => p.Id == projectId);

            if (project == null) return NotFound("Project not found.");

            bool isOwner = project.OwnerId == currentUserId;
            bool isAdmin = project.Memberships.Any(m => m.UserId == currentUserId && m.Role == ProjectRole.Admin);

            if (!isOwner && !isAdmin)
            {
                return Forbid();
            }

            var invitee = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
            if (invitee == null) return BadRequest("User with this email does not exist.");

            if (project.OwnerId == invitee.Id || project.Memberships.Any(m => m.UserId == invitee.Id))
            {
                return BadRequest("User is already a member of this project.");
            }

            var existingInvite = await _context.ProjectInvitations
                .FirstOrDefaultAsync(i => i.ProjectId == projectId && i.InviterEmail == request.Email && i.Status == InvitationStatus.Pending);

            if (existingInvite != null) return BadRequest("User already has a pending invitation.");

            var invitation = new ProjectInvitation
            {
                ProjectId = projectId,
                InviterId = currentUserId,
                InviterEmail = request.Email,
                Role = request.Role,
                Status = InvitationStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            _context.ProjectInvitations.Add(invitation);
            await _context.SaveChangesAsync();

            await _hubContext.Clients.Group($"User_{invitee.Id}").SendAsync("InvitationReceived", new
            {
                Id = invitation.Id,
                ProjectName = project.Name,
                InviterName = User.Identity!.Name,
                Role = invitation.Role.ToString()
            });

            return Ok(new { Message = "Invitation sent successfully." });
        }

        [HttpGet("my-pending")]
        public async Task<IActionResult> GetMyPendingInvitations()
        {
            var userEmail = User.FindFirst(ClaimTypes.Email)!.Value;

            var invitations = await _context.ProjectInvitations
                .Include(i => i.Project)
                .Include(i => i.Inviter)
                .Where(i => i.InviterEmail == userEmail && i.Status == InvitationStatus.Pending)
                .OrderByDescending(i => i.CreatedAt)
                .Select(i => new
                {
                    i.Id,
                    ProjectName = i.Project.Name,
                    InviterName = i.Inviter.Name,
                    Role = i.Role.ToString(),
                    i.CreatedAt
                })
                .ToListAsync();

            return Ok(invitations);
        }

        [HttpPost("{id}/accept")]
        public async Task<IActionResult> AcceptInvitation(int id)
        {
            var userEmail = User.FindFirst(ClaimTypes.Email)!.Value;
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            var invitation = await _context.ProjectInvitations
                .Include(i => i.Project)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (invitation == null) return NotFound();

            if (invitation.InviterEmail != userEmail) return Forbid();

            if (invitation.Status != InvitationStatus.Pending)
                return BadRequest("Invitation is no longer valid.");

            invitation.Status = InvitationStatus.Accepted;

            var membership = new ProjectMember
            {
                ProjectId = invitation.ProjectId,
                UserId = userId,
                Role = invitation.Role
            };
            _context.ProjectMembers.Add(membership);

            await _context.SaveChangesAsync();

            return Ok(new { Message = "Invitation accepted. You are now a member." });
        }

        [HttpPost("{id}/decline")]
        public async Task<IActionResult> DeclineInvitation(int id)
        {
            var userEmail = User.FindFirst(ClaimTypes.Email)!.Value;

            var invitation = await _context.ProjectInvitations.FindAsync(id);

            if (invitation == null) return NotFound();
            if (invitation.InviterEmail != userEmail) return Forbid();

            invitation.Status = InvitationStatus.Declined;
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Invitation declined." });
        }
    }
}
