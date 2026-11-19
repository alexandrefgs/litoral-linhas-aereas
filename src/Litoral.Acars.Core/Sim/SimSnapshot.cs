namespace Litoral.Acars.Core.Sim;

public sealed record SimSnapshot(
    DateTime At,
    double Latitude,
    double Longitude,
    int AltitudeFt,
    int AglFt,
    int IasKt,
    int GroundSpeedKt,
    int HeadingTrue,
    int VerticalSpeedFpm,
    double PitchDeg,
    int FlapsHandle,
    int FlapDetents,
    bool GearDown,
    bool OnGround,
    bool ParkingBrake,
    IReadOnlyList<bool> Engines,
    double FuelKg,
    int TouchdownRateFpm,
    bool Overspeed,
    bool Stall,
    bool Slew,
    bool Paused,
    double SimRate,
    bool Crashed)
{
    public bool AnyEngineRunning => Engines.Any(e => e);
}