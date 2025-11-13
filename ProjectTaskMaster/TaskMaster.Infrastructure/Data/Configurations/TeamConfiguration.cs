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
    public class TeamConfiguration : IEntityTypeConfiguration<Team>
    {
        public void Configure(EntityTypeBuilder<Team> builder)
        {
            builder.ToTable("Teams");


            builder.HasKey(t => t.Id);;
            builder.Property(t => t.Id).UseIdentityColumn();
            
            
            builder.Property(t => t.Name).IsRequired().HasMaxLength(100);
            builder.Property(t => t.Description).HasMaxLength(500);
            builder.Property(t => t.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            
            
            builder
                .HasMany(t => t.Projects)
                .WithMany(p => p.Teams)
                .UsingEntity<Dictionary<string, object>>(
                    "ProjectTeam",
                    j => j
                        .HasOne<Project>()
                        .WithMany()
                        .HasForeignKey("ProjectId")
                        .HasConstraintName("FK_ProjectTeam_Projects_ProjectId")
                        .OnDelete(DeleteBehavior.Cascade),
                    j => j
                        .HasOne<Team>()
                        .WithMany()
                        .HasForeignKey("TeamId")
                        .HasConstraintName("FK_ProjectTeam_Teams_TeamId")
                        .OnDelete(DeleteBehavior.ClientCascade),
                    j =>
                    {
                        j.HasKey("TeamId", "ProjectId");
                        j.ToTable("ProjectTeams");
                    });
        }
    }
}
