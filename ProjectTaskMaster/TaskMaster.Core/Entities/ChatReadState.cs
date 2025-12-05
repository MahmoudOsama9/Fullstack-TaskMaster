using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskMaster.Core.Entities
{
    public class ChatReadState
    {
        public int Id { get; set; }

        public int UserId { get; set; }
        public User User { get; set; } = default!;

        public int ProjectId { get; set; }
        public Project Project { get; set; } = default!;

        public DateTime LastReadAt { get; set; }
    }
}
