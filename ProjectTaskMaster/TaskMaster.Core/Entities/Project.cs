using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskMaster.Core.Enums;

namespace TaskMaster.Core.Entities
{
    public class Project
    {
        public int Id { get; set; }
        public string Name { get; set; } = default!;
        public string Description { get; set; } = default!;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime DueDate { get; set; }
        public string Status { get; set; } = Stage.NotStarted.ToString();

        public ICollection<ProjectMember> Memberships { get; set; } = new List<ProjectMember>();
        public int OwnerId { get; set; }
        public User Owner { get; set; } = default!;
        public ICollection<TaskItem> Tasks { get; set; } = new List<TaskItem>();
        public ICollection<Team> Teams { get; set; } = new List<Team>();
    }
}
