using LibreHardwareMonitor.Hardware;
using SensorWidget.Core;

namespace SensorWidget;

/// <summary>
/// Reads CPU, GPU and motherboard sensors through LibreHardwareMonitor. Read-only: it only updates
/// and reads sensors, and never touches a fan control. Opening is deferred to the first read so it
/// happens off the UI thread and its failure reaches the view model like any other read failure.
/// </summary>
public sealed class LibreHardwareSensorSource : ISensorSource, IDisposable
{
    private readonly object _gate = new();
    private Computer? _computer;
    private bool _isDisposed;

    public IReadOnlyList<RawSensor> ReadSensors()
    {
        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_isDisposed, this);
            _computer ??= OpenComputer();

            var sensors = new List<RawSensor>();
            foreach (var hardware in _computer.Hardware)
            {
                if (ToHardwareKind(hardware.HardwareType) is { } kind)
                {
                    ReadHardware(hardware, kind, sensors);
                }
            }

            return sensors;
        }
    }

    /// <summary>Waits for any running read, then closes the library and its driver handles.</summary>
    public void Dispose()
    {
        lock (_gate)
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;
            _computer?.Close();
            _computer = null;
        }
    }

    private static Computer OpenComputer()
    {
        var computer = new Computer
        {
            IsCpuEnabled = true,
            IsGpuEnabled = true,
            IsMotherboardEnabled = true,
        };

        try
        {
            computer.Open();
            return computer;
        }
        catch
        {
            computer.Close();
            throw;
        }
    }

    /// <summary>Sub-hardware, such as the motherboard's sensor chip, must be updated and read separately.</summary>
    private static void ReadHardware(IHardware hardware, HardwareKind kind, List<RawSensor> sensors)
    {
        hardware.Update();

        foreach (var sensor in hardware.Sensors)
        {
            if (ToSensorKind(sensor.SensorType) is { } sensorKind)
            {
                sensors.Add(new RawSensor(kind, sensorKind, sensor.Name, sensor.Value));
            }
        }

        foreach (var subHardware in hardware.SubHardware)
        {
            ReadHardware(subHardware, kind, sensors);
        }
    }

    private static HardwareKind? ToHardwareKind(HardwareType type) => type switch
    {
        HardwareType.Cpu => HardwareKind.Cpu,
        HardwareType.GpuAmd or HardwareType.GpuNvidia or HardwareType.GpuIntel => HardwareKind.Gpu,
        HardwareType.Motherboard => HardwareKind.Motherboard,
        _ => null,
    };

    private static SensorKind? ToSensorKind(SensorType type) => type switch
    {
        SensorType.Temperature => SensorKind.Temperature,
        SensorType.Fan => SensorKind.Fan,
        _ => null,
    };
}
