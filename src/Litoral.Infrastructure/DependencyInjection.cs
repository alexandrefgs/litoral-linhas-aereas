using System.Text;
using Litoral.Application.Admin;
using Litoral.Application.Auth;
using Litoral.Application.Common;
using Litoral.Application.Pilots;
using Litoral.Application.Reference;
using Litoral.Domain.Staff;
using Litoral.Infrastructure.Admin;
using Litoral.Infrastructure.Auth;
using Litoral.Infrastructure.Identity;
using Litoral.Infrastructure.Persistence;
using Litoral.Infrastructure.Pilots;
using Litoral.Infrastructure.Reference;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Litoral.Infrastructure.Storage;
using Litoral.Application.Bookings;
using Litoral.Infrastructure.Bookings;
using Litoral.Infrastructure.External;
using Litoral.Application.Scoring;
using Litoral.Infrastructure.Scoring;
using Litoral.Application.Flights;
using Litoral.Infrastructure.Flights;

namespace Litoral.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<LitoralDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("Litoral")));

        services
            .AddIdentityCore<ApplicationUser>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.Password.RequiredLength = 8;
                options.Password.RequireNonAlphanumeric = false;
            })
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<LitoralDbContext>();

        var jwtSection = configuration.GetSection(JwtOptions.SectionName);
        var jwt = jwtSection.Get<JwtOptions>()
            ?? throw new InvalidOperationException("'Jwt' configuration not found.");

        services.Configure<JwtOptions>(jwtSection);

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwt.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwt.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(1),
                    NameClaimType = "sub",
                    RoleClaimType = "role"
                };
            });

        services.AddAuthorization(options =>
        {
            foreach (var (policy, positions) in StaffPolicies.Positions)
                options.AddPolicy(policy, p => p.RequireRole(positions.Prepend(StaffPositions.CeoFounder)));
        });

        services.AddScoped<JwtTokenGenerator>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IPilotService, PilotService>();
        services.AddScoped<IReferenceService, ReferenceService>();
        services.AddScoped<IAirportAdminService, AirportAdminService>();
        services.AddScoped<IAirportLookupService, AirportLookupService>();
        services.AddHttpClient<AirportCatalogImporter>(client => client.Timeout = TimeSpan.FromMinutes(2));
        services.AddScoped<IRouteAdminService, RouteAdminService>();
        services.AddScoped<IFleetAdminService, FleetAdminService>();
        services.AddScoped<IScoringRuleService, ScoringRuleService>();
        services.AddSingleton<IPhotoStorage, LocalPhotoStorage>();
        services.AddHostedService<MaintenanceReleaseService>();
        services.AddScoped<BookingSupport>();
        services.AddScoped<IBookingService, BookingService>();
        services.AddScoped<IRosterService, RosterService>();
        services.AddScoped<IPilotFleetService, PilotFleetService>();
        services.AddScoped<IBriefingService, BriefingService>();
        services.AddHttpClient<IMetarProvider, MetarProvider>(client => client.Timeout = TimeSpan.FromSeconds(10));
        services.AddHttpClient<ISimBriefProvider, SimBriefProvider>(client => client.Timeout = TimeSpan.FromSeconds(15));
        services.AddHostedService<BookingExpiryService>();
        services.AddScoped<PirepFiling>();
        services.AddScoped<IFlightTrackingService, FlightTrackingService>();
        services.AddScoped<IPirepService, PirepService>();
        services.AddScoped<IFlightSimulator, FlightSimulator>();

        return services;
    }
}