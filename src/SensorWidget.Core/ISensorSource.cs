namespace SensorWidget.Core;

/// <summary>Reads the current value of every temperature and fan sensor.</summary>
public interface ISensorSource
{
    /// <summary>Takes a fresh reading. Blocking, so call it off the UI thread. Throws when sensors cannot be read.</summary>
    IReadOnlyList<RawSensor> ReadSensors();
}
