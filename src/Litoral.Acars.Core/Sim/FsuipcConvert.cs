namespace Litoral.Acars.Core.Sim;

public static class FsuipcConvert
{
    public const int FullControl = 16383;

    private const double TwoPow32 = 4294967296d;
    private const double FeetPerMeter = 3.280839895013123;
    private const double KnotsPerMps = 1.9438444924406046;
    private const double KgPerPound = 0.45359237;

    public static double Latitude(long raw) => raw * 90d / (10001750d * TwoPow32);

    public static double Longitude(long raw) => raw * 360d / (TwoPow32 * TwoPow32);

    public static int AltitudeFt(long raw) => (int)Math.Round(raw / TwoPow32 * FeetPerMeter);

    public static int RadioAltitudeFt(int raw) => Math.Max(0, (int)Math.Round(raw / 65536d * FeetPerMeter));

    public static int AirspeedKt(int raw) => Math.Max(0, (int)Math.Round(raw / 128d));

    public static int GroundSpeedKt(int raw) => Math.Max(0, (int)Math.Round(raw / 65536d * KnotsPerMps));

    public static int HeadingDeg(uint raw) => (int)Math.Round(raw * 360d / TwoPow32) % 360;

    public static int VerticalSpeedFpm(int raw) => (int)Math.Round(raw / 256d * FeetPerMeter * 60);

    public static double PitchDeg(int raw) => Math.Round(-raw * 360d / TwoPow32, 2);

    public static double SimRate(short raw) => raw / 256d;

    public static double PoundsToKg(double pounds) => pounds * KgPerPound;

    public static bool GearDown(int raw) => raw > FullControl / 2;

    public static bool IsSet(long raw) => raw != 0;
}