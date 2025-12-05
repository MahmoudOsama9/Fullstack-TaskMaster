using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskMaster.Core.Entities;

namespace TaskMaster.Infrastructure.Data.Configurations
{
    public class ChatReadStateConfiguration : IEntityTypeConfiguration<ChatReadState>
    {
        public void Configure(EntityTypeBuilder<ChatReadState> builder)
        {
            builder.HasKey(c => c.Id);

            builder.HasIndex(c => new { c.UserId, c.ProjectId }).IsUnique();
        }
    }
}
