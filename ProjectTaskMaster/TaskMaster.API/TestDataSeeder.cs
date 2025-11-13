using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;
using TaskMaster.Infrastructure.Data;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace TaskMaster.API;

public class TestDataSeeder : IHostedService
{
    private readonly IServiceProvider _serviceProvider;

    public TestDataSeeder(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<TaskMasterDbContext>();
        await context.Database.MigrateAsync(cancellationToken);

        var manager = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();

        if (await manager.FindByClientIdAsync("postman-client", cancellationToken) is null)
        {
            await manager.CreateAsync(new OpenIddictApplicationDescriptor
            {
                ClientId = "postman-client",
                ClientSecret = "postman-secret",
                DisplayName = "Postman Test Client",
                RedirectUris = { new Uri("https://oauth.pstmn.io/v1/callback") },
                Permissions =
                {
                    Permissions.Endpoints.Authorization,
                    Permissions.Endpoints.Token,
                    Permissions.GrantTypes.AuthorizationCode,
                    Permissions.GrantTypes.ClientCredentials,
                    Permissions.GrantTypes.RefreshToken,
                    Permissions.ResponseTypes.Code,
                    Permissions.Scopes.Email,
                    Permissions.Scopes.Profile,
                    Permissions.Scopes.Roles
                }
            }, cancellationToken);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}