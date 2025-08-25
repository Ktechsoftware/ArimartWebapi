    using Microsoft.OpenApi.Models;
    using Microsoft.IdentityModel.Tokens;
    using System.Text;
    using ArimartEcommerceAPI.Infrastructure.Data;
    using Microsoft.EntityFrameworkCore;
    using ArimartEcommerceAPI.Services.Services;
    using ArimartEcommerceAPI.Infrastructure.Data.Repositories;
    using ArimartEcommerceAPI.Infrastructure.Data.Hubs;
    using ArimartEcommerceAPI.API.Middleware;
    using ArimartEcommerceAPI.API.Services.BackgroundServices;
    using Hangfire;
    using Hangfire.SqlServer;
using Microsoft.Extensions.FileProviders;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

    builder.Services.AddEndpointsApiExplorer();
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Configure JSON to serialize enums as strings
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
builder.Services.AddSwaggerGen(options =>
    {
        options.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
        {
            In = ParameterLocation.Header,
            Name = "X-Api-Key",
            Type = SecuritySchemeType.ApiKey,
            Description = "Enter your API key"
        });

        options.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "ApiKey"
                    }
                },
                new string[] {}
            }
        });
    });

    builder.Services.AddAuthentication("Bearer")
        .AddJwtBearer("Bearer", options =>
        {
            var jwtSettings = builder.Configuration.GetSection("JwtSettings");
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtSettings["Issuer"],
                ValidAudience = jwtSettings["Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"]))
            };
        });

    builder.Services.AddHangfire(configuration => configuration
        .SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
        .UseSimpleAssemblyNameTypeSerializer()
        .UseRecommendedSerializerSettings()
        .UseSqlServerStorage(builder.Configuration.GetConnectionString("DefaultConnection"), new SqlServerStorageOptions
        {
            CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
            SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
            QueuePollInterval = TimeSpan.Zero,
            UseRecommendedIsolationLevel = true,
            DisableGlobalLocks = true
        }));
builder.Services.AddHangfireServer();

    builder.Services.AddAuthorization();
    builder.Services.AddControllers();
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

    builder.Services.AddHttpClient();
    builder.Services.AddHostedService<NotificationBackgroundService>();
    builder.Services.AddScoped<IAutomaticNotificationJob, AutomaticNotificationJob>();
    builder.Services.AddScoped<IFcmPushService, FcmPushService>();
    builder.Services.AddScoped<IOTPService, OTPService>();
    builder.Services.AddScoped<ITokenService, TokenService>();
    builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
    builder.Services.AddScoped<INotificationService, NotificationService>();
    builder.Services.AddSignalR();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigins", policy =>
    {
        policy
            .WithOrigins(
                "https://arimartreact.kuldeepchaurasia.in",
                "http://localhost:5173",
                "http://localhost:5015",
                "https://apiari.kuldeepchaurasia.in"
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials(); // if you need cookies/auth headers
    });
});


var app = builder.Build();
    app.UseStaticFiles();
    if (!app.Environment.IsDevelopment())
    {
        app.UseHttpsRedirection();
    }
// Serve static files from wwwroot
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(
        Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Uploads")),
    RequestPath = "/uploads",
    OnPrepareResponse = ctx =>
    {
        // Remove auth requirement for these files
        ctx.Context.Response.Headers.Append("Cache-Control", "public,max-age=604800");
    }
});

app.UseRouting();
app.UseCors("AllowSpecificOrigins");
app.UseSwagger();
    app.UseSwaggerUI();
    app.UseMiddleware<ApiKeyMiddleware>();
    app.UseAuthentication();
    app.UseAuthorization();
    app.UseHangfireDashboard("/hangfire", new DashboardOptions
    {
        Authorization = new[] { new AllowAllUsersAuthorizationFilter() }
    });

    app.MapGet("/", async context =>
    {
        context.Response.Redirect("/docs.html");
        await Task.CompletedTask;
    });

    app.UseEndpoints(endpoints =>
    {
        _ = endpoints.MapControllers();
        _ = endpoints.MapHub<NotificationHub>("/notificationHub");
    });

    using (var scope = app.Services.CreateScope())
    {
        var recurringJobManager = scope.ServiceProvider.GetRequiredService<IRecurringJobManager>();

        recurringJobManager.AddOrUpdate<IAutomaticNotificationJob>(
            "cart-abandonment",
            job => job.ProcessCartAbandonmentNotifications(),
            "*/30 * * * *");

        recurringJobManager.AddOrUpdate<IAutomaticNotificationJob>(
            "order-status",
            job => job.ProcessOrderStatusNotifications(),
            "*/15 * * * *");

        recurringJobManager.AddOrUpdate<IAutomaticNotificationJob>(
            "group-buy",
            job => job.ProcessGroupBuyNotifications(),
            "*/20 * * * *");

        recurringJobManager.AddOrUpdate<IAutomaticNotificationJob>(
            "recommendations",
            job => job.ProcessRecommendationNotifications(),
            "0 10,18 * * *");

        recurringJobManager.AddOrUpdate<IAutomaticNotificationJob>(
            "price-drops",
            job => job.ProcessPriceDropNotifications(),
            "0 9,15 * * *");

        recurringJobManager.AddOrUpdate<IAutomaticNotificationJob>(
            "inactive-users",
            job => job.ProcessInactiveUserNotifications(),
            "0 11 * * 1");

        recurringJobManager.AddOrUpdate<IAutomaticNotificationJob>(
            "birthday-wishes",
            job => job.ProcessBirthdayNotifications(),
            "0 9 * * *");

    recurringJobManager.AddOrUpdate<IAutomaticNotificationJob>("process-group-status",
        job => job.ProcessGroupStatusNotifications(),
        "*/10 * * * *"); // Every 10 minutes

    recurringJobManager.AddOrUpdate<IAutomaticNotificationJob>("process-enhanced-orders",
        job => job.ProcessEnhancedOrderNotifications(),
        "*/15 * * * *"); // Every 15 minutes
}

    app.Run();
