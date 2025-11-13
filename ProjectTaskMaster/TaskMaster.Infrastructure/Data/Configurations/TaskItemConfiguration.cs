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
    public class TaskItemConfiguration : IEntityTypeConfiguration<TaskItem>
    {
        public void Configure(EntityTypeBuilder<TaskItem> builder)
        {
            builder.ToTable("TaskItems");


            builder.HasKey(t => t.Id);
            builder.Property(t => t.Id).UseIdentityColumn();


            builder.Property(t => t.Title).IsRequired().HasMaxLength(100);
            builder.Property(t => t.Description).HasMaxLength(500);
            builder.Property(t => t.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            builder.Property(t => t.Status).IsRequired().HasMaxLength(50).HasDefaultValue(Stage.NotStarted);
            builder.Property(t => t.DueDate).IsRequired();
            builder.Property(t => t.Priority).IsRequired().HasDefaultValue(3);


            builder
                .HasOne(t => t.Project)
                .WithMany(p => p.Tasks)
                .HasForeignKey(t => t.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);


            builder
                .HasOne(t => t.AssignedToUser)
                .WithMany()
                .HasForeignKey(t => t.AssignedToUserId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}