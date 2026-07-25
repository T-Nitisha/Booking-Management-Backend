using BookingManagement.Application.Abstractions;
using BookingManagement.Application.Services;
using BookingManagement.Infrastructure.Auth;
using BookingManagement.Infrastructure.Events;
using BookingManagement.Infrastructure.Locking;
using BookingManagement.Infrastructure.Messaging;
using BookingManagement.Infrastructure.Persistence;
using BookingManagement.Infrastructure.Repositories;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace BookingManagement.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException("Missing 'ConnectionStrings:Postgres' configuration value.");

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddGoogleSsoAuthentication(configuration);
        services.AddBookingServices(configuration);

        return services;
    }

    private static IServiceCollection AddBookingServices(this IServiceCollection services, IConfiguration configuration)
    {
        var redisConnectionString = configuration.GetConnectionString("Redis")
            ?? throw new InvalidOperationException("Missing 'ConnectionStrings:Redis' configuration value.");

        services.AddSingleton<IConnectionMultiplexer>(_ =>
            ConnectionMultiplexer.Connect(redisConnectionString));

        services.AddScoped<IAppointmentBookingRepository, AppointmentBookingRepository>();
        services.AddScoped<IDistributedLockService, RedisDistributedLockService>();
        services.AddScoped<RedisBookingEventPublisher>();
        services.AddScoped<IBookingEventPublisher, CompositeBookingEventPublisher>();
        services.AddScoped<AppointmentBookingService>();

        services.AddKafkaMessaging(configuration);

        return services;
    }

    private static IServiceCollection AddKafkaMessaging(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<KafkaOptions>(configuration.GetSection(KafkaOptions.SectionName));

        var kafkaEnabled = configuration.GetValue<bool>($"{KafkaOptions.SectionName}:Enabled");
        if (kafkaEnabled)
        {
            services.AddSingleton<IKafkaBookingEventPublisher, KafkaBookingEventPublisher>();
        }
        else
        {
            services.AddSingleton<IKafkaBookingEventPublisher, NoOpKafkaBookingEventPublisher>();
        }

        return services;
    }

    private static IServiceCollection AddGoogleSsoAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var googleClientId = configuration["Authentication:Google:ClientId"]
            ?? throw new InvalidOperationException("Missing 'Authentication:Google:ClientId' configuration value.");

        services.AddScoped<IClaimsTransformation, AppRoleClaimsTransformation>();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                // Angular signs the user in with Google Identity Services and sends
                // the resulting Google ID token as a Bearer token. ASP.NET Core
                // validates it directly against Google's OIDC discovery document,
                // so no separate auth server (Auth0/IdentityServer) is required.
                options.Authority = "https://accounts.google.com";
                options.Audience = googleClientId;
                options.TokenValidationParameters.ValidateIssuer = true;
                options.TokenValidationParameters.ValidIssuers = new[]
                {
                    "https://accounts.google.com",
                    "accounts.google.com"
                };
            });

        services.AddAuthorization();

        return services;
    }
}

