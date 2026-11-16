using Litoral.Application.Admin;
using Litoral.Application.Common;
using Litoral.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Litoral.Infrastructure.Reference;

public sealed class AirportLookupService(LitoralDbContext db) : IAirportLookupService
{
    public async Task<AirportLookupDto> FindAsync(string icao, CancellationToken ct)
    {
        var code = (icao ?? string.Empty).Trim().ToUpperInvariant();

        return await db.AirportCatalog
            .AsNoTracking()
            .Where(a => a.Icao == code)
            .Select(a => new AirportLookupDto(a.Icao, a.Name, a.City, a.CountryName, a.CountryCode, a.Continent, a.Latitude, a.Longitude))
            .FirstOrDefaultAsync(ct)
            ?? throw new AppException(AppErrorKind.NotFound, $"Airport {code} was not found in the catalog.");
    }
}