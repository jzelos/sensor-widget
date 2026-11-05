namespace SensorWidget.Core;

/// <summary>The temperature, in °C, at or above which a reading counts as too hot (FR4.3).</summary>
public static class TemperatureLimits
{
    public const float Cpu = 85f;
    public const float Gpu = 90f;
    public const float Motherboard = 70f;

    public static float For(HardwareKind hardware) => hardware switch
    {
        HardwareKind.Cpu => Cpu,
        HardwareKind.Gpu => Gpu,
        HardwareKind.Motherboard => Motherboard,
        _ => throw new ArgumentOutOfRangeException(nameof(hardware), hardware, "Unknown hardware kind."),
    };
}
