using Microsoft.EntityFrameworkCore;
using TaskMaster.Core.Entities;
using TaskMaster.Infrastructure.Data;

namespace TaskMaster.API.GraphQL;

public record AddProjectInput(string Name, string Description, DateTime DueDate);

public record AddProjectPayload(Project Project);

public class Mutation
{
    public async Task<AddProjectPayload> AddProjectAsync(
        AddProjectInput input,
        [Service] IDbContextFactory<TaskMasterDbContext> contextFactory)
    {
        await using var context = await contextFactory.CreateDbContextAsync();

        var project = new Project
        {
            Name = input.Name,
            Description = input.Description,
            DueDate = input.DueDate
        };

        context.Projects.Add(project);
        await context.SaveChangesAsync();

        return new AddProjectPayload(project);
    }
}
