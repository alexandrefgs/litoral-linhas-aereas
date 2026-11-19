using Litoral.Application.Flights;

namespace Litoral.Api.IntegrationTests.Acars;

internal static class AcarsTestFlight
{
    public static StartFlightRequest Start(string network = "Vatsim") => new(network, "MSFS 2024", "Integration tests");

    public static PositionBatchRequest Climb(DateTime from, int flaps = 5, int ias = 200, int seconds = 30)
    {
        var positions = Enumerable.Range(0, seconds / 5 + 1)
            .Select(i => new PositionReport(from.AddSeconds(i * 5), -23.40 - i * 0.01, -46.50 - i * 0.01, 3000 + i * 300, null,
                ias, ias + 10, 210, 1800, flaps, false, false, "Climb"))
            .ToList();
        return new PositionBatchRequest(positions);
    }

    public static CompleteFlightRequest Complete(DateTime now, Action<Builder>? change = null)
    {
        var builder = new Builder(now);
        change?.Invoke(builder);
        return builder.Build();
    }

    public sealed class Builder(DateTime now)
    {
        public string ArrivalIcao { get; set; } = "SBFL";
        public int? LandingRateFpm { get; set; } = -140;
        public decimal? FuelAtPushback { get; set; } = 6500;
        public string? FuelUnit { get; set; } = "kg";
        public decimal? OnlinePercent { get; set; } = 100;
        public decimal? LiftoffPitch { get; set; } = 8.5m;
        public int? RotationSpeedKt { get; set; } = 148;
        public int? TakeoffFlaps { get; set; } = 5;
        public int? LandingFlaps { get; set; } = 30;
        public bool DiversionReported { get; set; }
        public List<FlightEventReport> Events { get; } = [];

        public CompleteFlightRequest Build() => new(
            ArrivalIcao,
            now.AddMinutes(-80),
            now.AddMinutes(-70),
            now.AddMinutes(-8),
            now.AddMinutes(-2),
            LandingRateFpm,
            FuelAtPushback,
            3200,
            FuelUnit,
            270,
            "UTRAP UZ24 NAXOP",
            35000,
            OnlinePercent,
            2,
            200,
            false,
            LiftoffPitch,
            RotationSpeedKt,
            TakeoffFlaps,
            LandingFlaps,
            1200,
            1500,
            DiversionReported,
            false,
            null,
            Events);
    }
}