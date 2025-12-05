using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TaskMaster.API.Hubs;
using TaskMaster.Core.Entities;
using TaskMaster.Core.Interfaces;
using TaskMaster.Infrastructure.Data;

namespace TaskMaster.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly IChatRepository _chatRepository;
    private readonly TaskMasterDbContext _context;
    private readonly IHubContext<ProjectUpdatesHub> _hubContext;
    private readonly ILogger<ChatController> _logger;

    public ChatController(
        IChatRepository chatRepository,
        TaskMasterDbContext context,
        IHubContext<ProjectUpdatesHub> hubContext,
        ILogger<ChatController> logger)
    {
        _chatRepository = chatRepository;
        _context = context;
        _hubContext = hubContext;
        _logger = logger;
    }

    public record ChatMessageDto(int Id, string Content, int SenderId, string SenderName, DateTime CreatedAt);
    public record SendMessageDto(string Content);

    // GET: api/Chat/project/{projectId}
    [HttpGet("project/{projectId}")]
    public async Task<ActionResult<IEnumerable<ChatMessageDto>>> GetProjectMessages(int projectId)
    {
        var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdString, out var userId)) return Unauthorized();

        // 1. Authorization Check
        var isMember = await _context.Projects.AnyAsync(p =>
            p.Id == projectId &&
            (p.OwnerId == userId || p.Memberships.Any(m => m.UserId == userId)));

        if (!isMember) return Forbid();

        // 2. Fetch Messages
        var messages = await _chatRepository.GetMessagesByProjectIdAsync(projectId);

        var dtos = messages.Select(m => new ChatMessageDto(
            m.Id, m.Content, m.SenderId, m.Sender.Name, m.CreatedAt
        )).ToList();

        // 3. Mark as Read (Persistent)
        await MarkReadInternal(userId, projectId);

        return Ok(dtos);
    }

    // POST: api/Chat/project/{projectId}
    [HttpPost("project/{projectId}")]
    public async Task<IActionResult> SendMessage(int projectId, [FromBody] SendMessageDto dto)
    {
        var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdString, out var userId)) return Unauthorized();

        if (string.IsNullOrWhiteSpace(dto.Content)) return BadRequest("Empty message.");

        // 1. Authorization Check
        var isMember = await _context.Projects.AnyAsync(p =>
            p.Id == projectId &&
            (p.OwnerId == userId || p.Memberships.Any(m => m.UserId == userId)));

        if (!isMember) return Forbid();

        // 2. Get Sender Name
        var senderName = User.FindFirst(ClaimTypes.Name)?.Value;
        if (string.IsNullOrEmpty(senderName))
        {
            var sender = await _context.Users.FindAsync(userId);
            senderName = sender?.Name ?? "Unknown User";
        }

        // 3. Save Message
        var message = new ChatMessage
        {
            ProjectId = projectId,
            SenderId = userId,
            Content = dto.Content,
            CreatedAt = DateTime.UtcNow
        };

        await _chatRepository.AddMessageAsync(message);

        // 4. Broadcast via SignalR
        // Include ProjectId so frontend can identify which chat this belongs to
        var broadcastDto = new
        {
            id = message.Id,
            content = message.Content,
            senderId = userId,
            senderName = senderName,
            createdAt = message.CreatedAt,
            projectId = projectId // Critical for frontend routing/unread logic
        };

        await _hubContext.Clients.Group(projectId.ToString()).SendAsync("ReceiveChatMessage", broadcastDto);

        return Ok(broadcastDto);
    }

    // POST: api/Chat/project/{projectId}/read
    [HttpPost("project/{projectId}/read")]
    public async Task<IActionResult> MarkAsRead(int projectId)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        await MarkReadInternal(userId, projectId);
        return Ok();
    }

    // GET: api/Chat/status
    // Returns a dictionary of { projectId: hasUnreadMessages } for the current user
    [HttpGet("status")]
    public async Task<ActionResult<Dictionary<int, bool>>> GetChatStatus()
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        // Get all projects the user is part of
        var projectIds = await _context.Projects
            .Where(p => p.OwnerId == userId || p.Memberships.Any(m => m.UserId == userId))
            .Select(p => p.Id)
            .ToListAsync();

        var result = new Dictionary<int, bool>();

        foreach (var pid in projectIds)
        {
            // Get last read time (default to min value if never read)
            var lastRead = await _context.ChatReadStates
                .Where(s => s.UserId == userId && s.ProjectId == pid)
                .Select(s => s.LastReadAt)
                .FirstOrDefaultAsync();

            if (lastRead == default) lastRead = DateTime.MinValue;

            // Check if any message exists that is NEWER than lastRead
            var hasUnread = await _context.ChatMessages
                .AnyAsync(m => m.ProjectId == pid && m.CreatedAt > lastRead);

            result.Add(pid, hasUnread);
        }

        return Ok(result);
    }

    private async Task MarkReadInternal(int userId, int projectId)
    {
        var state = await _context.ChatReadStates
            .FirstOrDefaultAsync(s => s.UserId == userId && s.ProjectId == projectId);

        if (state == null)
        {
            state = new ChatReadState { UserId = userId, ProjectId = projectId, LastReadAt = DateTime.UtcNow };
            _context.ChatReadStates.Add(state);
        }
        else
        {
            state.LastReadAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
    }
}