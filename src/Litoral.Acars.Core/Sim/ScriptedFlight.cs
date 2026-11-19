using Litoral.Acars.Core.Flight;

namespace Litoral.Acars.Core.Sim;

public sealed record ScriptedFlightOptions(
    string AircraftTypeIcao,
    double DepartureLatitude,
    double DepartureLongitude,
    double ArrivalLatitude,
    double ArrivalLongitude,
    int TakeoffFlaps = 5,
    int LandingFlaps = 30,
    int CruiseAltitudeFt = 10000,
    int TouchdownRateFpm = -150,
    double FuelKg = 6000);

public static class ScriptedFlight
{
    public static IEnumerable<SimSnapshot> Generate(DateTime start, ScriptedFlightOptions o)
    {
        var detents = FlapTable.Detents(o.AircraftTypeIcao);
        var at = start;
        var fuel = o.FuelKg;
        var progress = 0d;
        const int climbFpm = 2500;
        const int descentFpm = -1500;
        const int finalFpm = -700;
        var climbSeconds = o.CruiseAltitudeFt * 60 / climbFpm;
        var descentSeconds = (o.CruiseAltitudeFt - 3000) * 60 / -descentFpm;
        var finalSeconds = 3000 * 60 / -finalFpm;
        var airborneSeconds = climbSeconds + 180 + descentSeconds + finalSeconds;

        var state = new SimSnapshot(at, o.DepartureLatitude, o.DepartureLongitude, 0, 0, 0, 0, 90, 0, 0,
            0, detents, true, true, true, [false, false], fuel, 0, false, false, false, false, 1, false);

        SimSnapshot Step(Func<SimSnapshot, SimSnapshot> change, bool airborne = false)
        {
            at = at.AddSeconds(1);
            fuel -= state.AnyEngineRunning ? (airborne ? 1.2 : 0.2) : 0;
            if (airborne) progress = Math.Min(1, progress + 1d / airborneSeconds);
            state = change(state) with
            {
                At = at,
                FuelKg = fuel,
                Latitude = o.DepartureLatitude + (o.ArrivalLatitude - o.DepartureLatitude) * progress,
                Longitude = o.DepartureLongitude + (o.ArrivalLongitude - o.DepartureLongitude) * progress
            };
            state = state with { AltitudeFt = state.AglFt };
            return state;
        }

        int Flaps(int degrees) => FlapTable.ToHandle(o.AircraftTypeIcao, degrees);

        for (var t = 0; t < 60; t++)
            yield return Step(s => s with { Engines = [t >= 40, t >= 20] });

        for (var t = 0; t < 60; t++)
            yield return Step(s => s with { ParkingBrake = false, GroundSpeedKt = 3, IasKt = 3 });

        for (var t = 0; t < 180; t++)
            yield return Step(s => s with { GroundSpeedKt = 15, IasKt = 15, FlapsHandle = Flaps(o.TakeoffFlaps) });

        for (var ias = 20; ias <= 150; ias += 5)
            yield return Step(s => s with { IasKt = ias, GroundSpeedKt = ias, PitchDeg = ias >= 140 ? (ias - 135) / 2.5 : 0 });

        for (var t = 1; t <= climbSeconds; t++)
        {
            var agl = t * climbFpm / 60;
            yield return Step(s => s with
            {
                OnGround = false,
                AglFt = agl,
                VerticalSpeedFpm = climbFpm,
                IasKt = Math.Min(250, 155 + t),
                GroundSpeedKt = Math.Min(260, 160 + t),
                PitchDeg = 8,
                GearDown = agl < 100,
                FlapsHandle = agl < 1000 ? Flaps(o.TakeoffFlaps) : agl < 2000 && o.TakeoffFlaps > 1 ? Flaps(1) : 0
            }, airborne: true);
        }

        for (var t = 0; t < 180; t++)
            yield return Step(s => s with { AglFt = o.CruiseAltitudeFt, VerticalSpeedFpm = 0, IasKt = 250, GroundSpeedKt = 300, PitchDeg = 2 }, airborne: true);

        for (var t = 1; t <= descentSeconds; t++)
            yield return Step(s => s with { AglFt = o.CruiseAltitudeFt + t * descentFpm / 60, VerticalSpeedFpm = descentFpm, IasKt = 250, GroundSpeedKt = 280, PitchDeg = 0 }, airborne: true);

        for (var t = 1; t <= finalSeconds; t++)
        {
            var agl = 3000 + t * finalFpm / 60;
            yield return Step(s => s with
            {
                AglFt = Math.Max(10, agl),
                VerticalSpeedFpm = finalFpm,
                IasKt = 145,
                GroundSpeedKt = 140,
                PitchDeg = 3,
                GearDown = agl < 2500,
                FlapsHandle = agl < 1800 ? Flaps(o.LandingFlaps) : agl < 2500 ? Flaps(15) : Flaps(5)
            }, airborne: true);
        }

        for (var gs = 135; gs >= 20; gs -= 5)
            yield return Step(s => s with { OnGround = true, AglFt = 0, VerticalSpeedFpm = 0, TouchdownRateFpm = o.TouchdownRateFpm, IasKt = gs, GroundSpeedKt = gs, PitchDeg = 0 });

        for (var t = 0; t < 120; t++)
            yield return Step(s => s with { IasKt = 15, GroundSpeedKt = 15, FlapsHandle = 0 });

        for (var t = 0; t < 30; t++)
            yield return Step(s => s with { IasKt = 0, GroundSpeedKt = 0, ParkingBrake = t >= 5, Engines = [t < 20, t < 20] });
    }
}