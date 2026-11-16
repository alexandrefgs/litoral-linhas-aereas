using System.Data;
using Litoral.Application.Auth;
using Litoral.Application.Common;
using Litoral.Domain.Entities;
using Litoral.Domain.Enums;
using Litoral.Domain.Staff;
using Litoral.Infrastructure.Identity;
using Litoral.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Litoral.Infrastructure.Pilots;

namespace Litoral.Infrastructure.Auth;

public sealed class AuthService(
    LitoralDbContext db,
    UserManager<ApplicationUser> userManager,
    JwtTokenGenerator tokenGenerator) : IAuthService
{
    private const string DefaultHubIcao = "SBGR";

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken ct)
    {
        var email = request.Email.Trim();

        if (await userManager.FindByEmailAsync(email) is not null)
            throw new AppException(AppErrorKind.Conflict, "Este e-mail já está cadastrado.");

        var hubIcao = string.IsNullOrWhiteSpace(request.HubIcao)
            ? DefaultHubIcao
            : request.HubIcao.Trim().ToUpperInvariant();

        var isPassengerHub = await db.Hubs.AnyAsync(
            h => h.Airport.Icao == hubIcao && (h.Operations & OperationType.Passenger) == OperationType.Passenger, ct);

        if (!isPassengerHub)
            throw new AppException(AppErrorKind.Validation, $"{hubIcao} não é um hub de passageiros da Litoral.");

        var initialRank = await db.Ranks.OrderBy(r => r.Level).FirstAsync(ct);

        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);

        var isFirstPilot = !await db.Pilots.AnyAsync(ct);
        var lastNumber = await db.Pilots.MaxAsync(p => (int?)p.PilotNumber, ct);
        var pilotNumber = (lastNumber ?? Pilot.FirstPilotNumber - 1) + 1;

        var user = new ApplicationUser { UserName = email, Email = email };
        var created = await userManager.CreateAsync(user, request.Password);

        if (!created.Succeeded)
            throw new AppException(AppErrorKind.Validation, string.Join(" ", created.Errors.Select(e => e.Description)));

        var pilot = new Pilot(user.Id, pilotNumber, request.FirstName.Trim(), request.LastName.Trim(), initialRank, hubIcao);
        db.Pilots.Add(pilot);
        await db.SaveChangesAsync(ct);

        if (isFirstPilot)
        {
            var roleResult = await userManager.AddToRoleAsync(user, StaffPositions.CeoFounder);
            if (!roleResult.Succeeded)
                throw new InvalidOperationException("Não foi possível atribuir a posição de CEO & Founder.");
        }

        await transaction.CommitAsync(ct);

        return await BuildResponseAsync(user, pilot);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct)
    {
        var user = await userManager.FindByEmailAsync(request.Email.Trim());

        if (user is null || !await userManager.CheckPasswordAsync(user, request.Password))
            throw new AppException(AppErrorKind.Unauthorized, "E-mail ou senha inválidos.");

        var pilot = await LoadPilotAsync(user.Id, ct);
        return await BuildResponseAsync(user, pilot);
    }

    public async Task<PilotSummary> GetCurrentAsync(string userId, CancellationToken ct)
    {
        var user = await userManager.FindByIdAsync(userId)
            ?? throw new AppException(AppErrorKind.NotFound, "Usuário não encontrado.");

        var pilot = await LoadPilotAsync(userId, ct);
        var roles = await userManager.GetRolesAsync(user);

        return PilotSummaryMapper.ToSummary(user.Email!, pilot, roles.ToList());
    }

    private async Task<Pilot> LoadPilotAsync(string userId, CancellationToken ct) =>
        await db.Pilots.Include(p => p.Rank).FirstOrDefaultAsync(p => p.UserId == userId, ct)
        ?? throw new AppException(AppErrorKind.NotFound, "Piloto não encontrado.");

    private async Task<AuthResponse> BuildResponseAsync(ApplicationUser user, Pilot pilot)
    {
        var roles = (await userManager.GetRolesAsync(user)).ToList();
        var (token, expiresAt) = tokenGenerator.Generate(user, pilot, roles);

        return new AuthResponse(token, expiresAt, PilotSummaryMapper.ToSummary(user.Email!, pilot, roles));
    }
}