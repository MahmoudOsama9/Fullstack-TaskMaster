using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using StackExchange.Redis;
using System.Security.Claims;
using System.Text;
using TaskMaster.API;
using TaskMaster.API.GraphQL;
using TaskMaster.API.Hubs;
using TaskMaster.API.Middlewares;
using TaskMaster.API.Services;
using TaskMaster.Core.Interfaces;
using TaskMaster.Infrastructure.Data;
using TaskMaster.Infrastructure.Repositories;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

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
    Log.Information("Starting up the web host");

    var builder = WebApplication.CreateBuilder(args);

    var MyAllowSpecificOrigins = "_myAllowSpecificOrigins";

    builder.Services.AddCors(options =>
    {
        options.AddPolicy(name: MyAllowSpecificOrigins,
            policy =>
            {
                policy.WithOrigins("https://localhost:4200")
                      .AllowAnyHeader()
                      .AllowAnyMethod()
                      .AllowCredentials();
            });
    });



    builder.Host.UseSerilog();

    builder.WebHost.ConfigureKestrel(serverOptions =>
    {
        serverOptions.ListenLocalhost(5153, listenOptions =>
        {
            listenOptions.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http2;
            listenOptions.UseHttps();
        });
        serverOptions.ListenLocalhost(7189, listenOptions =>
        {
            listenOptions.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http1AndHttp2;
        });
    });

    //builder.Services.AddOpenIddict().AddCore(
    //    options =>
    //    {
    //        options.UseEntityFrameworkCore()
    //        .UseDbContext<TaskMasterDbContext>();
    //    })
    //    .AddServer(
    //    options =>
    //    {
    //        options.SetAuthorizationEndpointUris("connect/autherize")
    //        .SetTokenEndpointUris("connect/token")
    //        .SetUserInfoEndpointUris("connect/userinfo");

    //        options.AllowAuthorizationCodeFlow();

    //        options.AllowAuthorizationCodeFlow().RequireProofKeyForCodeExchange();

    //        options.AllowRefreshTokenFlow();

    //        options.AddDevelopmentEncryptionCertificate()
    //        .AddDevelopmentSigningCertificate();

    //        options.UseAspNetCore()
    //        .EnableTokenEndpointPassthrough()
    //        .EnableAuthorizationEndpointPassthrough()
    //        .EnableUserInfoEndpointPassthrough();
    //    })
    //    .AddValidation(
    //    options =>
    //        {
    //            options.UseLocalServer();
    //            options.UseAspNetCore();
    //        });

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
    builder.Services.AddSignalR().AddJsonProtocol(options =>
    {
        // This tells SignalR to send property names as camelCase (e.g., "projectId")
        // instead of PascalCase (e.g., "ProjectId") to match JavaScript conventions.
        options.PayloadSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });

    //var sqlConnectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    //builder.Services.AddDbContext<TaskMasterDbContext>(options =>
    //    options.UseSqlServer(sqlConnectionString));

    var sqlConnectionString = builder.Configuration.GetConnectionString("DefaultConnection");

    builder.Services.AddPooledDbContextFactory<TaskMasterDbContext>(options =>
    options.UseSqlServer(sqlConnectionString));

    builder.Services.AddScoped(sp => sp.GetRequiredService<IDbContextFactory<TaskMasterDbContext>>().CreateDbContext());

    //builder.Services.AddAuthentication(options =>
    //{
    //    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    //    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    //})
    //.AddJwtBearer(options =>
    //{
    //    options.TokenValidationParameters = new TokenValidationParameters
    //    {
    //        ValidateIssuer = true,
    //        ValidateAudience = true,
    //        ValidateLifetime = true,
    //        ValidateIssuerSigningKey = true,
    //        ValidIssuer = builder.Configuration["Jwt:Issuer"],
    //        ValidAudience = builder.Configuration["Jwt:Audience"],
    //        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)),
    //        NameClaimType = ClaimTypes.NameIdentifier
    //    };
    //});

    //builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    //.AddJwtBearer(options =>
    //{
    //    options.TokenValidationParameters = new TokenValidationParameters
    //    {
    //        ValidateIssuer = true,
    //        ValidateAudience = true,
    //        ValidateLifetime = true,
    //        ValidateIssuerSigningKey = true,
    //        ValidIssuer = builder.Configuration["Jwt:Issuer"],
    //        ValidAudience = builder.Configuration["Jwt:Audience"],
    //        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)),
    //        NameClaimType = ClaimTypes.NameIdentifier
    //    };
    //})
    //.AddGoogle(options =>
    //{
    //    options.ClientId = builder.Configuration["Authentication:Google:ClientId"]!;
    //    options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"]!;
    //});

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

    builder.Services.AddRateLimiter(options =>
    {
        options.AddFixedWindowLimiter(policyName: "auth", opt =>
        {
            opt.PermitLimit = 5;
            opt.Window = TimeSpan.FromMinutes(1);
        });
        options.OnRejected = (context, token) =>
        {
            context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            return new ValueTask();
        };
    });

    var redisConnectionString = builder.Configuration.GetConnectionString("Redis");

    builder.Services.AddSingleton<IConnectionMultiplexer>(
        ConnectionMultiplexer.Connect(redisConnectionString!));

    builder.Services.AddScoped<ProjectRepository>();

    builder.Services.AddScoped<IProjectRepository, CachedProjectRepository>(sp =>
    {
        var projectRepository = sp.GetRequiredService<ProjectRepository>();
        var multiplexer = sp.GetRequiredService<IConnectionMultiplexer>();
        var logger = sp.GetRequiredService<ILogger<CachedProjectRepository>>();
        return new CachedProjectRepository(projectRepository, multiplexer, logger);
    });

    builder.Services.AddHostedService<TestDataSeeder>();

    builder.Services
    .AddGraphQLServer()
    .AddAuthorization()
    .AddQueryType<Query>()
    .AddTypeExtension<ProjectQueries>()
    .AddMutationType<Mutation>()
    .ModifyOptions(options => options.StrictValidation = true)
    .AddProjections()
    .AddFiltering()
    .AddSorting();

    builder.Services.AddControllers().AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;

        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    });
    builder.Services.AddEndpointsApiExplorer();

    builder.Services.AddSwaggerGen(options =>
    {
        options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            In = ParameterLocation.Header,
            Description = "Please enter a valid token",
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            BearerFormat = "JWT",
            Scheme = "Bearer"
        });
        options.AddSecurityRequirement(new OpenApiSecurityRequirement
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
                new string[]{}
            }
        });
    });

    builder.Services.AddGrpc();

    builder.Services.AddGrpcReflection();

    var app = builder.Build();

    app.UseMiddleware<CorrelationIdMiddleware>();

    app.UseSerilogRequestLogging();
    app.UseRateLimiter();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();

        app.MapGrpcReflectionService();
    }
    app.UseHttpsRedirection();
    
    app.UseRouting();

    app.UseCors(MyAllowSpecificOrigins);



    app.UseAuthentication();
    app.UseAuthorization();

    app.MapHub<ProjectUpdatesHub>("/project-updates");

    app.MapGraphQL("/graphql")
.RequireAuthorization();

    app.MapGrpcService<ProjectReporterService>();

    app.MapControllers();





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