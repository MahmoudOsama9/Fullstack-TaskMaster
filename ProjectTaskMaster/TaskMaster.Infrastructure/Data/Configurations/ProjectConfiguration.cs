using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskMaster.Core.Entities;
using TaskMaster.Core.Enums;

namespace TaskMaster.Infrastructure.Data.Configurations
{
    public class ProjectConfiguration : IEntityTypeConfiguration<Project>
    {
        public void Configure(EntityTypeBuilder<Project> builder)
        {
            builder.ToTable("Projects");


            builder.HasKey(p => p.Id);
            builder.Property(p => p.Id).UseIdentityColumn();


            builder.Property(p => p.Name).IsRequired().HasMaxLength(100);
            builder.Property(p => p.Description).HasMaxLength(500);
            builder.Property(p => p.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            builder.Property(p => p.Status).IsRequired().HasMaxLength(50).HasDefaultValue("NotStarted");
            builder.Property(p => p.DueDate).IsRequired();


            builder
                .HasMany(p => p.Teams)
                .WithMany(t => t.Projects)
                .UsingEntity<Dictionary<string, object>>(
                    "ProjectTeam",
                    j => j
                        .HasOne<Team>()
                        .WithMany()
                        .HasForeignKey("TeamId")
                        .HasConstraintName("FK_ProjectTeam_Teams_TeamId")
                        .OnDelete(DeleteBehavior.Cascade),
                    j => j
                        .HasOne<Project>()
                        .WithMany()
                        .HasForeignKey("ProjectId")
                        .HasConstraintName("FK_ProjectTeam_Projects_ProjectId")
                        .OnDelete(DeleteBehavior.ClientCascade),
                    j =>
                    {
                        j.HasKey("ProjectId", "TeamId");
                        j.ToTable("ProjectTeams");
                    });

            builder.HasOne(p => p.Owner)
               .WithMany()
               .HasForeignKey(p => p.OwnerId)
               .OnDelete(DeleteBehavior.Restrict);

            builder
                .HasMany(p => p.Tasks)
                .WithOne(t => t.Project)
                .HasForeignKey(t => t.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
