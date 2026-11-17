using Litoral.Application.Admin;
using Litoral.Application.Common;
using Litoral.Application.Pilots;
using Litoral.Domain.Entities;
using Litoral.Domain.Enums;
using Litoral.Infrastructure.Identity;
using Litoral.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Litoral.Infrastructure.Pilots;

public sealed class PilotSettingsService(
    LitoralDbContext db,
    UserManager<ApplicationUser> userManager,
    IPhotoStorage storage,
    PilotEraser eraser) : IPilotSettingsService
{
    private const string PhotoFolder = "pilots";
    private const string CoverFolder = "covers";

    public async Task<PilotSettingsDto> GetAsync(string userId, CancellationToken ct) =>
        await ToDtoAsync(await FindPilotAsync(userId, ct), ct);

    public async Task<PilotSettingsDto> UpdatePersonalAsync(string userId, UpdatePersonalRequest request, CancellationToken ct)
    {
        var pilot = await FindPilotAsync(userId, ct);

        if (!Enum.TryParse<NameFormat>(request.NameFormat, out var format))
            throw new AppException(AppErrorKind.Validation, "Invalid name format.");

        pilot.UpdatePersonal(request.FirstName, request.LastName, request.BirthDate, request.CountryCode, request.State, request.City);
        pilot.SetNameFormat(format);
        pilot.SetNetworkIds(request.VatsimId, request.IvaoId, request.DiscordId);

        if (pilot.VatsimId is not null && await db.Pilots.AnyAsync(p => p.Id != pilot.Id && p.VatsimId == pilot.VatsimId, ct))
            throw new AppException(AppErrorKind.Conflict, "This VATSIM ID is already registered to another pilot.");
        if (pilot.IvaoId is not null && await db.Pilots.AnyAsync(p => p.Id != pilot.Id && p.IvaoId == pilot.IvaoId, ct))
            throw new AppException(AppErrorKind.Conflict, "This IVAO ID is already registered to another pilot.");

        await db.SaveChangesAsync(ct);
        return await ToDtoAsync(pilot, ct);
    }

    public async Task<PilotSettingsDto> UpdateConfigAsync(string userId, UpdateConfigRequest request, CancellationToken ct)
    {
        var pilot = await FindPilotAsync(userId, ct);

        var codes = (request.SimAircraft ?? []).Select(c => c.Trim().ToUpperInvariant()).Distinct().ToList();
        var known = await db.AircraftTypes.Where(t => codes.Contains(t.IcaoCode)).Select(t => t.IcaoCode).Distinct().ToListAsync(ct);
        var unknown = codes.Except(known).ToList();
        if (unknown.Count > 0)
            throw new AppException(AppErrorKind.Validation, $"Unknown aircraft type: {string.Join(", ", unknown)}.");

        pilot.SetSimBriefUsername(request.SimBriefUsername);
        pilot.SetSimAircraft(codes);
        await db.SaveChangesAsync(ct);

        return await ToDtoAsync(pilot, ct);
    }

    public async Task ChangePasswordAsync(string userId, ChangePasswordRequest request, CancellationToken ct)
    {
        var user = await userManager.FindByIdAsync(userId)
            ?? throw new AppException(AppErrorKind.NotFound, "User not found.");

        var result = await userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        if (result.Succeeded) return;

        if (result.Errors.Any(e => e.Code == "PasswordMismatch"))
            throw new AppException(AppErrorKind.Validation, "The current password is incorrect.");

        throw new AppException(AppErrorKind.Validation, string.Join(" ", result.Errors.Select(e => e.Description)));
    }

    public async Task DeleteAccountAsync(string userId, DeleteAccountRequest request, CancellationToken ct)
    {
        var user = await userManager.FindByIdAsync(userId)
            ?? throw new AppException(AppErrorKind.NotFound, "User not found.");

        if (string.IsNullOrEmpty(request.Password) || !await userManager.CheckPasswordAsync(user, request.Password))
            throw new AppException(AppErrorKind.Validation, "The password is incorrect.");

        var pilot = await FindPilotAsync(userId, ct);
        if (await eraser.BlockReasonAsync(pilot, true, ct) is string reason)
            throw new AppException(AppErrorKind.Conflict, reason);

        await eraser.DeleteAsync(pilot, ct);
    }

    public async Task<PilotSettingsDto> UploadPhotoAsync(string userId, PhotoUpload upload, CancellationToken ct)
    {
        var pilot = await FindPilotAsync(userId, ct);
        var url = await storage.SaveAsync(PhotoFolder, upload, ct);
        var previous = pilot.SetPhoto(url);
        await db.SaveChangesAsync(ct);
        storage.Delete(previous);
        return await ToDtoAsync(pilot, ct);
    }

    public async Task<PilotSettingsDto> RemovePhotoAsync(string userId, CancellationToken ct)
    {
        var pilot = await FindPilotAsync(userId, ct);
        var previous = pilot.SetPhoto(null);
        await db.SaveChangesAsync(ct);
        storage.Delete(previous);
        return await ToDtoAsync(pilot, ct);
    }

    public async Task<PilotSettingsDto> UploadCoverAsync(string userId, PhotoUpload upload, CancellationToken ct)
    {
        var pilot = await FindPilotAsync(userId, ct);
        var url = await storage.SaveAsync(CoverFolder, upload, ct);
        var previous = pilot.SetCover(url);
        await db.SaveChangesAsync(ct);
        storage.Delete(previous);
        return await ToDtoAsync(pilot, ct);
    }

    public async Task<PilotSettingsDto> RemoveCoverAsync(string userId, CancellationToken ct)
    {
        var pilot = await FindPilotAsync(userId, ct);
        var previous = pilot.SetCover(null);
        await db.SaveChangesAsync(ct);
        storage.Delete(previous);
        return await ToDtoAsync(pilot, ct);
    }

    private async Task<Pilot> FindPilotAsync(string userId, CancellationToken ct) =>
        await db.Pilots.Include(p => p.Rank).FirstOrDefaultAsync(p => p.UserId == userId, ct)
        ?? throw new AppException(AppErrorKind.NotFound, "Pilot not found.");

    private async Task<PilotSettingsDto> ToDtoAsync(Pilot pilot, CancellationToken ct)
    {
        var user = await userManager.FindByIdAsync(pilot.UserId);

        var types = await db.AircraftTypes.AsNoTracking()
            .OrderBy(t => t.IsCargo).ThenBy(t => t.Name)
            .Select(t => new { t.IcaoCode, t.Name })
            .ToListAsync(ct);

        var aircraftTypes = types
            .GroupBy(t => t.IcaoCode)
            .Select(g => new SimAircraftOptionDto(g.Key, g.First().Name))
            .OrderBy(t => t.Name)
            .ToList();

        var nameFormats = Enum.GetValues<NameFormat>()
            .Select(f => new NameFormatOptionDto(f.ToString(), pilot.FormatName(f)))
            .ToList();

        return new PilotSettingsDto(
            pilot.FirstName,
            pilot.LastName,
            pilot.NameFormat.ToString(),
            nameFormats,
            user?.Email ?? "",
            pilot.BirthDate,
            pilot.CountryCode,
            pilot.State,
            pilot.City,
            pilot.VatsimId,
            pilot.IvaoId,
            pilot.DiscordId,
            pilot.SimBriefUsername,
            pilot.SimAircraftList,
            aircraftTypes,
            pilot.PhotoUrl,
            pilot.CoverUrl);
    }
}