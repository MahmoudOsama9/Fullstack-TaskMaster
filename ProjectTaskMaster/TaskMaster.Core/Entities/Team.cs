using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskMaster.Core.Entities
{
    public class Team
    {
        public int Id { get; set; }
        public string Name { get; set; } = default!;
        public string Description { get; set; } = default!;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string Title { get; set; } = default!;

        public ICollection<Project> Projects { get; set; } = new List<Project>();
        public ICollection<User> Members { get; set; } = new List<User>();
    }
}
