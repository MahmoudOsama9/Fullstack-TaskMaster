using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskMaster.Core.Entities;

namespace TaskMaster.Infrastructure.Data.Configurations
{
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.ToTable("Users");
            
            builder.HasKey(u => u.Id);
            builder.Property(u => u.Id).UseIdentityColumn();

            builder.HasIndex(u => u.Email).IsUnique();
            builder.Property(u => u.Email).IsRequired().HasMaxLength(150);
            builder.Property(u => u.Name).IsRequired().HasMaxLength(100);
            builder.Property(u => u.PasswordHash).IsRequired().HasMaxLength(500);
            builder.Property(u => u.CreatedAt).HasDefaultValueSql("GETUTCDATE()");


            builder
                .HasMany(u => u.Teams)
                .WithMany(t => t.Members)
                .UsingEntity<Dictionary<string, object>>(
                    "UserTeam",
                    j => j
                        .HasOne<Team>()
                        .WithMany()
                        .HasForeignKey("TeamId")
                        .HasConstraintName("FK_UserTeam_Teams_TeamId")
                        .OnDelete(DeleteBehavior.Cascade),
                    j => j
                        .HasOne<User>()
                        .WithMany()
                        .HasForeignKey("UserId")
                        .HasConstraintName("FK_UserTeam_Users_UserId")
                        .OnDelete(DeleteBehavior.ClientCascade),
                    j =>
                    {
                        j.HasKey("UserId", "TeamId");
                        j.ToTable("UserTeams");
                    });
        }
    }
}
