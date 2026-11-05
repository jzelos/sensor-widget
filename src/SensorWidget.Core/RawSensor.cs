namespace SensorWidget.Core;

/// <summary>The part of the PC a sensor belongs to.</summary>
public enum HardwareKind
{
    Cpu,
    Gpu,
    Motherboard,
}

/// <summary>What a sensor measures.</summary>
public enum SensorKind
{
    Temperature,
    Fan,
}

/// <summary>One sensor as the hardware reports it. Value is null when the sensor returned nothing.</summary>
public sealed record RawSensor(HardwareKind Hardware, SensorKind Sensor, string Name, float? Value)
{
    /// <summary>True when the sensor returned a real number.</summary>
    public bool HasValue => Value is { } value && float.IsFinite(value);
}
