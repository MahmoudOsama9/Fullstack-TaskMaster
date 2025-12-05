using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskMaster.Core.Enums;

namespace TaskMaster.Core.Entities
{
    public class TaskItem
    {
        public int Id { get; set; }
        public string Title { get; set; } = default!;
        public string Description { get; set; } = default!;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime DueDate { get; set; }
        public string Status { get; set; } = Stage.NotStarted.ToString();
        public int Priority { get; set; } = 3;

        public ICollection<TaskNote> Notes { get; set; } = new List<TaskNote>();
        public int ProjectId { get; set; }
        public Project Project { get; set; } = default!;
        public int? AssignedToUserId { get; set; }
        public User? AssignedToUser { get; set; }
    }
}
