using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace TaskMaster.Core.Entities
{
    public class TaskNote
    {
        public int Id { get; set; }
        public string Content { get; set; } = default!;
        public DateTime CreatedAt { get; set; }
        public int TaskId { get; set; }
        public TaskItem TaskItem { get; set; } = default!;
        public int AutherId { get; set; } = default!;
        public User User { get; set; } = default!;
    }
}
