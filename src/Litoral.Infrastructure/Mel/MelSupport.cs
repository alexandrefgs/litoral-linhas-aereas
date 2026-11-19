using Litoral.Application.Mel;
using Litoral.Domain.Entities;

namespace Litoral.Infrastructure.Mel;

public static class MelSupport
{
    public static MelItemDto ToDto(MelItem m, int timesOpened) => new(
        m.Id,
        m.Code,
        m.Kind.ToString(),
        m.Title,
        m.Category.ToString(),
        m.CategoryADays,
        m.Days,
        m.Restriction,
        m.Procedure,
        m.Verification.ToString(),
        m.AcarsCheck.ToString(),
        m.CheckValue,
        m.CheckText,
        m.FuelPenaltyPercent,
        m.AircraftTypeList,
        m.AutoEligible,
        m.IsActive,
        timesOpened);

    public static OpenMelItemDto ToDto(AircraftMelItem a, DateTime now) => new(
        a.Id,
        a.AircraftId,
        a.Aircraft.Registration,
        a.Aircraft.AircraftType.IcaoCode,
        a.Aircraft.CurrentAirport.Icao,
        a.MelItem.Code,
        a.MelItem.Kind.ToString(),
        a.MelItem.Title,
        a.MelItem.Category.ToString(),
        a.MelItem.Restriction,
        a.MelItem.Procedure,
        a.MelItem.Verification.ToString(),
        a.MelItem.FuelPenaltyPercent,
        a.Source.ToString(),
        a.OpenedBy,
        a.Note,
        a.OpenedAt,
        a.DueAt,
        a.IsExpired(now));

    public static PilotMelItemDto ToPilotDto(AircraftMelItem a) => new(
        a.MelItem.Code,
        a.MelItem.Kind.ToString(),
        a.MelItem.Title,
        a.MelItem.Category.ToString(),
        a.MelItem.Restriction,
        a.MelItem.Procedure,
        a.MelItem.Verification.ToString(),
        a.MelItem.FuelPenaltyPercent,
        a.DueAt);

    public static string Label(MelItem item) => $"{(item.Kind == Domain.Enums.MelKind.Cdl ? "CDL" : "MEL")} {item.Code} · {item.Title}";
}