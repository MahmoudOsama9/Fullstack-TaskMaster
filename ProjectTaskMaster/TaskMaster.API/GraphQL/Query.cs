using TaskMaster.Core.Entities;
using TaskMaster.Infrastructure.Data;
using HotChocolate;
using HotChocolate.Data;
using HotChocolate.Types;
using HotChocolate.Resolvers;
using HotChocolate.Execution;
using HotChocolate.Authorization;

namespace TaskMaster.API.GraphQL;

[ExtendObjectType(OperationTypeNames.Query)]
[Authorize]
public class ProjectQueries
{
    [UsePaging]
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<Project> GetProjects([Service] TaskMasterDbContext context)
    {
        return context.Projects;
    }

    [UseProjection]
    public IQueryable<Project> GetProjectById([Service] TaskMasterDbContext context, int id)
    {
        return context.Projects.Where(p => p.Id == id);
    }
}