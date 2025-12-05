using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskMaster.Core.Enums;

namespace TaskMaster.Core.Entities
{
    public class ProjectInvitation
    {
        public int Id { get; set; }
        public int ProjectId { get; set; }
        public Project Project { get; set; } = default!;
        public int InviterId { get; set; }
        public User Inviter { get; set; } = default!;
        public string InviterEmail { get; set; } = string.Empty;
        public ProjectRole Role { get; set; } = ProjectRole.Viewer;
        public InvitationStatus Status { get; set; } = InvitationStatus.Pending;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    }
}
