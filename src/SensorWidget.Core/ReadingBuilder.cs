using System.Globalization;

namespace SensorWidget.Core;

/// <summary>Turns raw sensors into the rows the widget shows.</summary>
public static class ReadingBuilder
{
    public const string MissingValue = "N/A";

    private const string PreferredCpuSensor = "Core (Tctl/Tdie)";
    private const string PreferredGpuSensor = "GPU Core";

    private const string CpuFanLabel = "CPU Fan";
    private const string HiddenMotherboardTemperature = "Temperature #1";

    /// <summary>
    /// What the library's generic names measure on the one targeted board, an MSI MAG X570S
    /// TOMAHAWK MAX WIFI, where the library has no names of its own. Names must match exactly.
    /// </summary>
    private static readonly Dictionary<string, string> MotherboardLabels = new(StringComparer.Ordinal)
    {
        ["Temperature #2"] = "Motherboard",
        ["Temperature #3"] = "VRM MOSFETs",
        ["Temperature #5"] = "AUXTIN2",
        ["Fan #2"] = CpuFanLabel,
    };

    /// <summary>
    /// Always produces the CPU and GPU temperature rows, so a missing sensor still shows as N/A, then
    /// one row for each motherboard temperature and fan that is not hidden. The CPU fan is wired to a
    /// motherboard header but belongs with the CPU, so it follows the CPU temperature. There is no GPU
    /// fan row, whatever the GPU reports, because the targeted system is watercooled and has no GPU fan.
    /// </summary>
    public static IReadOnlyList<Reading> Build(IReadOnlyCollection<RawSensor> sensors)
    {
        ArgumentNullException.ThrowIfNull(sensors);

        var readings = new List<Reading>
        {
            ToReading(HardwareKind.Cpu, SensorKind.Temperature, "CPU Temperature",
                PickSensor(sensors, HardwareKind.Cpu, SensorKind.Temperature, PreferredCpuSensor)),
        };

        readings.AddRange(CpuFanReadings(sensors));
        readings.Add(ToReading(HardwareKind.Gpu, SensorKind.Temperature, "GPU Temperature",
            PickSensor(sensors, HardwareKind.Gpu, SensorKind.Temperature, PreferredGpuSensor)));
        readings.AddRange(MotherboardReadings(sensors, SensorKind.Temperature));
        readings.AddRange(MotherboardReadings(sensors, SensorKind.Fan));
        return readings;
    }

    /// <summary>Groups rows by hardware for display, keeping CPU, GPU, motherboard order.</summary>
    public static IReadOnlyList<ReadingGroup> Group(IReadOnlyCollection<Reading> readings)
    {
        ArgumentNullException.ThrowIfNull(readings);

        return readings
            .GroupBy(reading => reading.Hardware)
            .OrderBy(group => group.Key)
            .Select(group => new ReadingGroup(GroupName(group.Key), group.ToList()))
            .ToList();
    }

    /// <summary>A stopped CPU fan matters, so these rows are never hidden, even at 0 RPM.</summary>
    private static IEnumerable<Reading> CpuFanReadings(IEnumerable<RawSensor> sensors) =>
        sensors
            .Where(IsCpuFan)
            .Select(sensor => ToReading(HardwareKind.Cpu, SensorKind.Fan, CpuFanLabel, sensor));

    private static IEnumerable<Reading> MotherboardReadings(IEnumerable<RawSensor> sensors, SensorKind kind) =>
        sensors
            .Where(sensor => sensor.Hardware == HardwareKind.Motherboard && sensor.Sensor == kind)
            .Where(sensor => !IsCpuFan(sensor) && !IsHiddenOnMotherboard(sensor))
            .Select(sensor => ToReading(HardwareKind.Motherboard, kind, MotherboardLabel(sensor), sensor));

    private static bool IsCpuFan(RawSensor sensor) =>
        sensor.Hardware == HardwareKind.Motherboard
        && sensor.Sensor == SensorKind.Fan
        && MotherboardLabel(sensor) == CpuFanLabel;

    private static string MotherboardLabel(RawSensor sensor) =>
        MotherboardLabels.GetValueOrDefault(sensor.Name, sensor.Name);

    /// <summary>
    /// Hides one temperature and any fan reading exactly 0 RPM, which is an empty header or a
    /// stopped fan. A fan with no value still shows as N/A.
    /// </summary>
    private static bool IsHiddenOnMotherboard(RawSensor sensor) => sensor.Sensor == SensorKind.Temperature
        ? string.Equals(sensor.Name, HiddenMotherboardTemperature, StringComparison.Ordinal)
        : sensor.Value == 0f;

    /// <summary>Picks the preferred sensor when it has a value, otherwise the first that has one.</summary>
    private static RawSensor? PickSensor(
        IEnumerable<RawSensor> sensors, HardwareKind hardware, SensorKind kind, string? preferredName)
    {
        var candidates = sensors
            .Where(sensor => sensor.Hardware == hardware && sensor.Sensor == kind && sensor.HasValue)
            .ToList();

        return candidates.Find(sensor => string.Equals(sensor.Name, preferredName, StringComparison.OrdinalIgnoreCase))
            ?? candidates.FirstOrDefault();
    }

    private static Reading ToReading(HardwareKind hardware, SensorKind kind, string label, RawSensor? sensor)
    {
        if (sensor is null || !sensor.HasValue)
        {
            return new Reading(hardware, label, MissingValue, IsTooHot: false);
        }

        var value = sensor.Value!.Value;
        return kind == SensorKind.Temperature
            ? new Reading(hardware, label, FormatTemperature(value), value >= TemperatureLimits.For(hardware))
            : new Reading(hardware, label, FormatFanSpeed(value), IsTooHot: false);
    }

    private static string FormatTemperature(float celsius) =>
        string.Create(CultureInfo.InvariantCulture, $"{celsius:F1} °C");

    private static string FormatFanSpeed(float rpm) =>
        string.Create(CultureInfo.InvariantCulture, $"{rpm:F0} RPM");

    private static string GroupName(HardwareKind hardware) => hardware switch
    {
        HardwareKind.Cpu => "CPU",
        HardwareKind.Gpu => "GPU",
        HardwareKind.Motherboard => "Motherboard",
        _ => throw new ArgumentOutOfRangeException(nameof(hardware), hardware, "Unknown hardware kind."),
    };
}
