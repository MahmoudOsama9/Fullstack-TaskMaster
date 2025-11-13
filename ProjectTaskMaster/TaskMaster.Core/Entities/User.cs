using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskMaster.Core.Entities
{
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; } = default!;
        public string Email { get; set; } = default!;
        public string PasswordHash { get; set; } = default!;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public string? RefreshToken { get; set; }
        public DateTime RefreshExpiryTime { get; set; }

        public ICollection<Team> Teams { get; set; } = new List<Team>();
    }
}
