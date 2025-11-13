using Microsoft.EntityFrameworkCore;
using OpenIddict.EntityFrameworkCore;
using TaskMaster.Core.Entities;

namespace TaskMaster.Infrastructure.Data
{
    public class TaskMasterDbContext : DbContext
    {
        public TaskMasterDbContext(DbContextOptions<TaskMasterDbContext> options) : base(options)
        {
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfigurationsFromAssembly(typeof(TaskMasterDbContext).Assembly);

            modelBuilder.UseOpenIddict();
        }
        public DbSet<User> Users { get; set; } = default!;
        public DbSet<Project> Projects { get; set; } = default!;
        public DbSet<TaskItem> TaskItems { get; set; } = default!;
        public DbSet<Team> Teams { get; set; } = default!;
    }
}
