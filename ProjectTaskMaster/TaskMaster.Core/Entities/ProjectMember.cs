using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskMaster.Core.Enums;

namespace TaskMaster.Core.Entities
{
    public class ProjectMember
    {
        public int ProjectId { get; set; }
        public Project Project { get; set; } = default!;
        public int UserId { get; set; }
        public User User { get; set; } = default!;

        public ProjectRole Role { get; set; }
    }
}
