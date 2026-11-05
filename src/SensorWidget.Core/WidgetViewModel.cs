using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SensorWidget.Core;

/// <summary>What the widget window shows. Refreshes from a sensor source and survives its failures.</summary>
public sealed class WidgetViewModel : INotifyPropertyChanged
{
    public const string ReadingStartedMessage = "Reading sensors...";
    public const string UnavailableMessage = "Readings are unavailable. Trying again every second.";

    private readonly ISensorSource _source;
    private readonly ErrorLog _errorLog;
    private IReadOnlyList<Reading> _readings = Array.Empty<Reading>();
    private IReadOnlyList<ReadingGroup> _readingGroups = Array.Empty<ReadingGroup>();
    private string? _statusMessage = ReadingStartedMessage;
    private bool _isRefreshing;
    private bool _isInFailure;

    public WidgetViewModel(ISensorSource source, ErrorLog errorLog)
    {
        _source = source ?? throw new ArgumentNullException(nameof(source));
        _errorLog = errorLog ?? throw new ArgumentNullException(nameof(errorLog));
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public IReadOnlyList<Reading> Readings
    {
        get => _readings;
        private set => SetField(ref _readings, value);
    }

    public IReadOnlyList<ReadingGroup> ReadingGroups
    {
        get => _readingGroups;
        private set => SetField(ref _readingGroups, value);
    }

    /// <summary>A plain message for the person, or null when there is nothing to say.</summary>
    public string? StatusMessage
    {
        get => _statusMessage;
        private set => SetField(ref _statusMessage, value);
    }

    /// <summary>
    /// Reads the sensors off the calling thread and updates the readings. Does nothing while an
    /// earlier read is still running, and never throws for a sensor failure.
    /// </summary>
    public async Task RefreshAsync()
    {
        if (_isRefreshing)
        {
            return;
        }

        _isRefreshing = true;
        try
        {
            IReadOnlyList<RawSensor> sensors;
            try
            {
                sensors = await Task.Run(ReadUsableSensors);
            }
            catch (Exception failure)
            {
                // The boundary with the sensor library: whatever it throws, the widget stays open (FR5.2).
                ShowFailure(failure);
                return;
            }

            ShowReadings(sensors);
        }
        finally
        {
            _isRefreshing = false;
        }
    }

    /// <summary>A source that answers with no usable value at all has failed, as when the driver cannot load.</summary>
    private IReadOnlyList<RawSensor> ReadUsableSensors()
    {
        var sensors = _source.ReadSensors();
        if (!sensors.Any(sensor => sensor.HasValue))
        {
            throw new InvalidOperationException("The sensor source returned no usable values.");
        }

        return sensors;
    }

    private void ShowReadings(IReadOnlyList<RawSensor> sensors)
    {
        var readings = ReadingBuilder.Build(sensors);
        Readings = readings;
        ReadingGroups = ReadingBuilder.Group(readings);
        StatusMessage = null;
        _isInFailure = false;
    }

    /// <summary>Clears old values so they cannot pass for current ones, and logs once per run of failures.</summary>
    private void ShowFailure(Exception failure)
    {
        Readings = Array.Empty<Reading>();
        ReadingGroups = Array.Empty<ReadingGroup>();
        StatusMessage = UnavailableMessage;

        if (!_isInFailure)
        {
            _errorLog.Write(failure);
        }

        _isInFailure = true;
    }

    private void SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = "")
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return;
        }

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
