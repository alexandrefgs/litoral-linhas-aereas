using System.Text;
using Litoral.Application.Admin;
using Litoral.Application.Auth;
using Litoral.Application.Bookings;
using Litoral.Application.Common;
using Litoral.Application.Flights;
using Litoral.Application.Pilots;
using Litoral.Application.Reference;
using Litoral.Application.Scoring;
using Litoral.Application.Statistics;
using Litoral.Domain.Staff;
using Litoral.Infrastructure.Admin;
using Litoral.Infrastructure.Audit;
using Litoral.Infrastructure.Auth;
using Litoral.Infrastructure.Bookings;
using Litoral.Infrastructure.External;
using Litoral.Infrastructure.Flights;
using Litoral.Infrastructure.Identity;
using Litoral.Infrastructure.Persistence;
using Litoral.Infrastructure.Pilots;
using Litoral.Infrastructure.Reference;
using Litoral.Infrastructure.Scoring;
using Litoral.Infrastructure.Statistics;
using Litoral.Infrastructure.Storage;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Litoral.Application.Airports;
using Litoral.Infrastructure.Airports;
using Litoral.Application.Operations;
using Litoral.Infrastructure.Operations;
using Microsoft.AspNetCore.Authentication;

namespace Litoral.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<AuditContext>();
        services.AddScoped<AuditInterceptor>();
        services.AddScoped<IAuditLog, AuditLogWriter>();
        services.AddScoped<IAuditLogQuery, AuditLogQuery>();

        services.AddDbContext<LitoralDbContext>((provider, options) =>
            options.UseSqlServer(configuration.GetConnectionString("Litoral"))
                .AddInterceptors(provider.GetRequiredService<AuditInterceptor>()));

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

        services.AddScoped<StaffRoles>();
        services.AddScoped<IClaimsTransformation, StaffClaimsTransformation>();
        services.AddScoped<PilotEraser>();
        services.AddScoped<IPilotAdminService, PilotAdminService>();
        services.AddScoped<IStaffAdminService, StaffAdminService>();
        services.AddScoped<JwtTokenGenerator>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IPilotSettingsService, PilotSettingsService>();
        services.AddScoped<IProfileService, ProfileService>();
        services.AddScoped<IAirportDirectoryService, AirportDirectoryService>();
        services.AddScoped<IReferenceService, ReferenceService>();
        services.AddScoped<IAirportAdminService, AirportAdminService>();
        services.AddScoped<IAirportLookupService, AirportLookupService>();
        services.AddHttpClient<AirportCatalogImporter>(client => client.Timeout = TimeSpan.FromMinutes(2));
        services.AddScoped<IRouteAdminService, RouteAdminService>();
        services.AddScoped<IFleetAdminService, FleetAdminService>();
        services.AddScoped<IScoringRuleService, ScoringRuleService>();
        services.AddSingleton<IPhotoStorage, LocalPhotoStorage>();
        services.AddHostedService<MaintenanceReleaseService>();
        services.AddScoped<IStatisticsService, StatisticsService>();
        services.AddScoped<IPointsStatementService, PointsStatementService>();
        services.AddHostedService<MonthlyAwardService>();
        services.AddScoped<BookingSupport>();
        services.AddScoped<IBookingService, BookingService>();
        services.AddScoped<IRosterService, RosterService>();
        services.AddScoped<IPilotFleetService, PilotFleetService>();
        services.AddScoped<IBriefingService, BriefingService>();
        services.AddHttpClient<IMetarProvider, MetarProvider>(client => client.Timeout = TimeSpan.FromSeconds(15));
        services.Configure<SimBriefOptions>(configuration.GetSection(SimBriefOptions.SectionName));
        services.AddHttpClient<ISimBriefProvider, SimBriefProvider>(client => client.Timeout = TimeSpan.FromSeconds(15));
        services.AddHostedService<BookingExpiryService>();
        services.AddScoped<PirepFiling>();
        services.AddScoped<IFlightTrackingService, FlightTrackingService>();
        services.AddScoped<IPirepService, PirepService>();
        services.AddScoped<CrewCards>();
        services.AddScoped<IFlightDetailService, FlightDetailService>();
        services.AddScoped<IPirepReviewService, PirepReviewService>();
        services.AddScoped<IFlightSimulator, FlightSimulator>();
        services.AddScoped<SpecialFlightSupport>();
        services.AddScoped<MaintenanceFerryPlanner>();
        services.AddScoped<ISpecialFlightAdminService, SpecialFlightAdminService>();
        services.AddScoped<ISpecialFlightService, SpecialFlightService>();

        return services;
    }
}