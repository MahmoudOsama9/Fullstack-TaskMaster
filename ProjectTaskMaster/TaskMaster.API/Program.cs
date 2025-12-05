using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using StackExchange.Redis;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using TaskMaster.API;
using TaskMaster.API.GraphQL;
using TaskMaster.API.Hubs;
using TaskMaster.API.Middlewares;
using TaskMaster.API.Services;
using TaskMaster.Core.Interfaces;
using TaskMaster.Infrastructure.Data;
using TaskMaster.Infrastructure.Repositories;

// --- Serilog Bootstrap ---
Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .ReadFrom.Configuration(new ConfigurationBuilder()
        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
        .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true)
        .Build())
    .CreateLogger();

try
{
    Log.Information("Starting TaskMaster API host");
    var builder = WebApplication.CreateBuilder(args);

    var isMigrationMode = args.Any(a => a.Contains("migrations", StringComparison.OrdinalIgnoreCase));
    var disableHttps = Environment.GetEnvironmentVariable("DISABLE_HTTPS")?.ToLower() == "true";
    var inContainer = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true";

    builder.Host.UseSerilog();

    var sqlConnectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    builder.Services.AddDbContext<TaskMasterDbContext>(options =>
        options.UseSqlServer(sqlConnectionString));

    if (!isMigrationMode)
    {
        builder.WebHost.ConfigureKestrel(options =>
        {
            if (!disableHttps && !inContainer)
            {
                options.ListenLocalhost(5153, listen => listen.UseHttps());
                options.ListenLocalhost(7200);
            }
            else
            {
                options.ListenAnyIP(80);
            }
        });

        builder.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy.WithOrigins("https://localhost:4200", "http://localhost:4200")
                      .AllowAnyHeader()
                      .AllowAnyMethod()
                      .AllowCredentials();
            });
        });

        builder.Services.AddControllers().AddJsonOptions(opts =>
        {
            opts.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            opts.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
        });

        // --- Authentication ---
        builder.Services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme)
        .AddGoogle(GoogleDefaults.AuthenticationScheme, options =>
        {
            options.ClientId = builder.Configuration["Authentication:Google:ClientId"]!;
            options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"]!;
            options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = builder.Configuration["Jwt:Issuer"],
                ValidAudience = builder.Configuration["Jwt:Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)),
                NameClaimType = ClaimTypes.NameIdentifier
            };
        });

        builder.Services.AddAuthorization();

        // --- Redis ---
        builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
        {
            var redisConnectionString = builder.Configuration.GetConnectionString("Redis");
            var configuration = ConfigurationOptions.Parse(redisConnectionString!);
            configuration.AbortOnConnectFail = false;
            return ConnectionMultiplexer.Connect(configuration);
        });

        // --- Repositories ---
        builder.Services.AddScoped<IInvitationRepository, InvitationRepository>();
        builder.Services.AddScoped<ITaskRepository, TaskRepository>();
        builder.Services.AddScoped<IChatRepository, ChatRepository>();
        builder.Services.AddScoped<ProjectRepository>();
        builder.Services.AddScoped<IProjectRepository>(sp =>
        {
            var realRepo = sp.GetRequiredService<ProjectRepository>();
            var redis = sp.GetRequiredService<IConnectionMultiplexer>();
            var logger = sp.GetRequiredService<ILogger<CachedProjectRepository>>();
            return new CachedProjectRepository(realRepo, redis, logger);
        });

        // --- GraphQL, gRPC, SignalR ---
        builder.Services.AddGraphQLServer()
               .AddAuthorization()
               .AddQueryType<ProjectQueries>()
               .AddMutationType<Mutation>()
               .AddProjections().AddFiltering().AddSorting();

        builder.Services.AddGrpc();
        builder.Services.AddGrpcReflection();
        builder.Services.AddSignalR().AddJsonProtocol(opts =>
        {
            opts.PayloadSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        });

        // --- Swagger ---
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(opt =>
        {
            opt.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                In = ParameterLocation.Header,
                Description = "Please enter a valid token",
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                BearerFormat = "JWT",
                Scheme = "Bearer"
            });
            opt.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type=ReferenceType.SecurityScheme,
                            Id="Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });
        });

        // --- OpenIddict ---
        builder.Services.AddOpenIddict()
            .AddCore(options =>
            {
                options.UseEntityFrameworkCore()
                       .UseDbContext<TaskMasterDbContext>();
            })
            .AddServer(options =>
            {
                options.SetAuthorizationEndpointUris("connect/authorize")
                       .SetTokenEndpointUris("connect/token")
                       .SetUserInfoEndpointUris("connect/userinfo");

                options.AllowClientCredentialsFlow();
                options.AllowAuthorizationCodeFlow().RequireProofKeyForCodeExchange();
                options.AllowRefreshTokenFlow();

                options.AddDevelopmentEncryptionCertificate()
                       .AddDevelopmentSigningCertificate();

                options.UseAspNetCore()
                       .EnableTokenEndpointPassthrough()
                       .EnableAuthorizationEndpointPassthrough()
                       .EnableUserInfoEndpointPassthrough();
            });
    }

    var app = builder.Build();

    // --- Migrations Mode ---
    if (isMigrationMode)
    {
        Console.WriteLine("🔧 Running EF Core Migrations...");
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TaskMasterDbContext>();
        db.Database.Migrate();
        Console.WriteLine("✅ EF Migrations completed successfully.");
        return;
    }

    // --- Middleware Pipeline ---
    app.UseMiddleware<CorrelationIdMiddleware>();
    app.UseSerilogRequestLogging();

    if (app.Environment.IsDevelopment() && !inContainer)
    {
        app.UseHttpsRedirection();
        app.UseSwagger();
        app.UseSwaggerUI();
        app.MapGrpcReflectionService();
    }

    if (!disableHttps && !inContainer)
        app.UseHttpsRedirection();

    app.UseRouting();
    app.UseCors();
    app.UseAuthentication();
    app.UseAuthorization();

    // --- Endpoints ---
    app.MapControllers();
    app.MapGraphQL("/graphql").RequireAuthorization();
    app.MapHub<ProjectUpdatesHub>("/project-updates").RequireAuthorization();
    app.MapGrpcService<ProjectReporterService>();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Host terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
