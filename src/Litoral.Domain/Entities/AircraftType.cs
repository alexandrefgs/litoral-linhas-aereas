using Litoral.Domain.Common;

namespace Litoral.Domain.Entities;

public sealed class AircraftType : Entity
{
    public string IcaoCode { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public bool IsCargo { get; private set; }
    public int MaxRangeNm { get; private set; }

    private AircraftType() { }

    public AircraftType(string icaoCode, string name, bool isCargo, int maxRangeNm)
    {
        if (string.IsNullOrWhiteSpace(icaoCode) || icaoCode.Length != 4)
            throw new DomainException("Aircraft ICAO code must have 4 characters.");
        if (maxRangeNm <= 0)
            throw new DomainException("Range must be greater than zero.");

        IcaoCode = icaoCode.ToUpperInvariant();
        Name = name;
        IsCargo = isCargo;
        MaxRangeNm = maxRangeNm;
    }
}