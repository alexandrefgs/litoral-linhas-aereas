using FSUIPC;
using Litoral.Acars.Core.Sim;

namespace Litoral.Acars.Sim;

public sealed class FsuipcSimSource : IDisposable
{
    private const string Group = "litoral";

    private readonly Offset<long> _latitude = new(Group, 0x0560);
    private readonly Offset<long> _longitude = new(Group, 0x0568);
    private readonly Offset<long> _altitude = new(Group, 0x0570);
    private readonly Offset<int> _radioAltitude = new(Group, 0x31E4);
    private readonly Offset<int> _ias = new(Group, 0x02BC);
    private readonly Offset<int> _groundSpeed = new(Group, 0x02B4);
    private readonly Offset<uint> _heading = new(Group, 0x0580);
    private readonly Offset<int> _verticalSpeed = new(Group, 0x02C8);
    private readonly Offset<int> _pitch = new(Group, 0x0578);
    private readonly Offset<int> _flapsHandle = new(Group, 0x0BDC);
    private readonly Offset<short> _flapDetents = new(Group, 0x3BF8);
    private readonly Offset<int> _gearHandle = new(Group, 0x0BE8);
    private readonly Offset<short> _onGround = new(Group, 0x0366);
    private readonly Offset<short> _parkingBrake = new(Group, 0x0BC8);
    private readonly Offset<short> _engineCount = new(Group, 0x0AEC);
    private readonly Offset<short>[] _engines =
    [
        new(Group, 0x0894),
        new(Group, 0x092C),
        new(Group, 0x09C4),
        new(Group, 0x0A5C)
    ];
    private readonly Offset<int> _fuelPounds = new(Group, 0x126C);
    private readonly Offset<int> _touchdownVerticalSpeed = new(Group, 0x030C);
    private readonly Offset<byte> _stall = new(Group, 0x036C);
    private readonly Offset<byte> _overspeed = new(Group, 0x036D);
    private readonly Offset<short> _slew = new(Group, 0x05DC);
    private readonly Offset<short> _paused = new(Group, 0x0264);
    private readonly Offset<short> _simRate = new(Group, 0x0C1A);
    private readonly Offset<short> _crashed = new(Group, 0x0840);
    private readonly Offset<string> _title = new(Group, 0x3D00, 256);

    public bool IsConnected => FSUIPCConnection.IsOpen;
    public string? Simulator { get; private set; }
    public bool IsSupported => SimulatorName.IsSupported(Simulator);
    public string? AircraftTitle { get; private set; }

    public bool TryConnect()
    {
        if (FSUIPCConnection.IsOpen) return true;

        try
        {
            FSUIPCConnection.Open();
            Simulator = SimulatorName.Describe(FSUIPCConnection.FlightSimVersionConnected.ToString());
            return true;
        }
        catch (FSUIPCException)
        {
            Simulator = null;
            return false;
        }
    }

    public SimSnapshot? Read(DateTime at)
    {
        if (!FSUIPCConnection.IsOpen) return null;

        try
        {
            FSUIPCConnection.Process(Group);
        }
        catch (FSUIPCException)
        {
            FSUIPCConnection.Close();
            Simulator = null;
            return null;
        }

        AircraftTitle = _title.Value?.Trim();
        var engineCount = Math.Clamp((int)_engineCount.Value, 1, _engines.Length);

        return new RawSimData(
            _latitude.Value,
            _longitude.Value,
            _altitude.Value,
            _radioAltitude.Value,
            _ias.Value,
            _groundSpeed.Value,
            _heading.Value,
            _verticalSpeed.Value,
            _pitch.Value,
            _flapsHandle.Value,
            _flapDetents.Value,
            _gearHandle.Value,
            _onGround.Value,
            _parkingBrake.Value,
            _engines.Take(engineCount).Select(e => e.Value).ToList(),
            _fuelPounds.Value,
            _touchdownVerticalSpeed.Value,
            _overspeed.Value,
            _stall.Value,
            _slew.Value,
            _paused.Value,
            _simRate.Value,
            _crashed.Value).ToSnapshot(at);
    }

    public void Dispose()
    {
        if (FSUIPCConnection.IsOpen) FSUIPCConnection.Close();
    }
}