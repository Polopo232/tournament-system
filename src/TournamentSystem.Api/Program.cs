using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.Filters;
using System.Reflection;
using System.Security.Claims;
using System.Text;
using TournamentSystem.Api.Hubs;
using TournamentSystem.Api.Middleware;
using TournamentSystem.Api.Services;
using TournamentSystem.Application.Interfaces;
using TournamentSystem.Application.Services;
using TournamentSystem.Infrastructure.Data;
using TournamentSystem.Infrastructure.External;
using TournamentSystem.Infrastructure.Repositories;
using TournamentSystem.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

if (string.IsNullOrWhiteSpace(builder.Configuration["Jwt:Key"]))
{
    throw new InvalidOperationException(
        "Jwt:Key is not configured. Set it with: dotnet user-secrets set \"Jwt:Key\" \"<value>\" --project src/TournamentSystem.Api");
}

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ITournamentRepository, TournamentRepository>();
builder.Services.AddScoped<IMatchRepository, MatchRepository>();
builder.Services.AddScoped<IRatingRepository, RatingRepository>();

builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();

builder.Services.AddScoped<ITournamentService, TournamentService>();
builder.Services.AddScoped<IBracketService, BracketService>();

builder.Services.AddScoped<ITournamentImportService, TournamentImportService>();
builder.Services.AddTransient<LichessRateLimitHandler>();

builder.Services.AddHttpClient<IExternalArenaSource, LichessArenaSource>(client =>
{
    client.BaseAddress = new Uri("https://lichess.org");
    client.DefaultRequestHeaders.UserAgent.ParseAdd("TournamentSystem/1.0 (school project)");
})
.AddHttpMessageHandler<LichessRateLimitHandler>();

builder.Services.AddSingleton<EloCalculatorService>();
builder.Services.AddScoped<IMatchService, MatchService>();
builder.Services.AddScoped<IRatingService, RatingService>();

builder.Services.AddSignalR();
builder.Services.AddScoped<ITournamentNotifier, SignalRTournamentNotifier>();

builder.Services.AddScoped<ITournamentFinishService, TournamentFinishService>();

const string ReactCorsPolicy = "ReactClientPolicy";

builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendPolicy", policy =>
    {
        policy.WithOrigins("http://localhost:5173", "http://127.0.0.1:5173")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;

                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/ws/tournament"))
                {
                    context.Token = accessToken;
                }

                return Task.CompletedTask;
            }
        };
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var users = context.HttpContext.RequestServices.GetRequiredService<IUserRepository>();
                var idClaim = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);

                if (!int.TryParse(idClaim, out var userId))
                {
                    context.Fail("Invalid token.");
                    return;
                }

                var user = await users.GetByIdAsync(userId);
                if (user == null || user.IsBlocked)
                {
                    context.Fail("Account is blocked or no longer exists.");
                }
            }
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddControllers();

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Tournament System API",
        Version = "v1",
        Description = "Turniirisüsteemi Web API dokumentatsioon"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Sisesta oma JWT token."
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });

    var xmlFiles = Directory.GetFiles(AppContext.BaseDirectory, "*.xml", SearchOption.TopDirectoryOnly);
    foreach (var xmlFile in xmlFiles)
    {
        options.IncludeXmlComments(xmlFile, includeControllerXmlComments: true);
    }

    options.ExampleFilters();
});

builder.Services.AddSwaggerExamplesFromAssemblies(
    Assembly.GetExecutingAssembly(),
    typeof(IUserService).Assembly
);

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    if (app.Environment.IsDevelopment())
    {
        await DbInitializer.SeedAsync(db);
    }
    else
    {
        await db.Database.MigrateAsync();
    }
}

app.UseExceptionHandler();

app.UseCors("FrontendPolicy");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Tournament System API v1");
    });
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<TournamentHub>("/ws/tournament");

app.Run();