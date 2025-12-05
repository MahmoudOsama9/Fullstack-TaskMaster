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
    public class ProjectMemberConfiguration : IEntityTypeConfiguration<ProjectMember>
    {
        public void Configure(EntityTypeBuilder<ProjectMember> builder) {
            builder.HasKey(pm => new { pm.ProjectId, pm.UserId});

            builder.HasOne(pm => pm.Project)
                .WithMany(p => p.Memberships)
                .HasForeignKey(p => p.ProjectId);

            builder.HasOne(pm => pm.User)
                .WithMany()
                .HasForeignKey(p => p.UserId);
        }
    }
}
