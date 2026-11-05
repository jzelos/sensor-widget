using SensorWidget.Core;

namespace SensorWidget.Tests;

public class WidgetViewModelTests : IDisposable
{
    private readonly string _logFolder = Path.Combine(
        AppContext.BaseDirectory, "viewmodel-test-logs", Guid.NewGuid().ToString("N"));

    private string LogPath => Path.Combine(_logFolder, "errors.log");

    public void Dispose()
    {
        if (Directory.Exists(_logFolder))
        {
            Directory.Delete(_logFolder, recursive: true);
        }

        GC.SuppressFinalize(this);
    }

    private WidgetViewModel CreateViewModel(ISensorSource source) => new(source, new ErrorLog(LogPath));

    private static IReadOnlyList<RawSensor> CpuAt(float celsius) => new[]
    {
        new RawSensor(HardwareKind.Cpu, SensorKind.Temperature, "Core (Tctl/Tdie)", celsius),
    };

    private static Reading CpuRow(WidgetViewModel viewModel) =>
        Assert.Single(viewModel.Readings, reading => reading.Label == "CPU Temperature");

    // FR2: each refresh shows the newest values.
    [Fact]
    public async Task RefreshAsync_UpdatesReadingsFromTheSource()
    {
        var source = new FakeSensorSource(CpuAt(40f), CpuAt(41.5f));
        var viewModel = CreateViewModel(source);

        await viewModel.RefreshAsync();
        Assert.Equal("40.0 °C", CpuRow(viewModel).DisplayValue);

        await viewModel.RefreshAsync();
        Assert.Equal("41.5 °C", CpuRow(viewModel).DisplayValue);
        Assert.Null(viewModel.StatusMessage);
    }

    [Fact]
    public async Task RefreshAsync_RaisesChangeNotificationsForTheWindow()
    {
        var viewModel = CreateViewModel(new FakeSensorSource(CpuAt(40f)));
        var changed = new List<string?>();
        viewModel.PropertyChanged += (_, args) => changed.Add(args.PropertyName);

        await viewModel.RefreshAsync();

        Assert.Contains(nameof(WidgetViewModel.ReadingGroups), changed);
        Assert.Contains(nameof(WidgetViewModel.StatusMessage), changed);
    }

    // FR4.2: the highlight clears on the first refresh below the limit.
    [Fact]
    public async Task RefreshAsync_ClearsHighlightWhenValueFallsBelowLimit()
    {
        var viewModel = CreateViewModel(new FakeSensorSource(CpuAt(85f), CpuAt(84.9f)));

        await viewModel.RefreshAsync();
        Assert.True(CpuRow(viewModel).IsTooHot);

        await viewModel.RefreshAsync();
        Assert.False(CpuRow(viewModel).IsTooHot);
    }

    // FR5.2: a total failure leaves the widget alive, with a message and no old values.
    [Fact]
    public async Task RefreshAsync_KeepsWorkingWithAMessageWhenTheSourceFails()
    {
        var viewModel = CreateViewModel(new FakeSensorSource(CpuAt(40f), new InvalidOperationException("driver failed")));

        await viewModel.RefreshAsync();
        await viewModel.RefreshAsync();

        Assert.Equal(WidgetViewModel.UnavailableMessage, viewModel.StatusMessage);
        Assert.Empty(viewModel.Readings);
        Assert.Contains("driver failed", File.ReadAllText(LogPath));
    }

    // FR5.2: readings come back on their own after a failure.
    [Fact]
    public async Task RefreshAsync_RecoversAfterAFailure()
    {
        var viewModel = CreateViewModel(new FakeSensorSource(new InvalidOperationException("driver failed"), CpuAt(40f)));

        await viewModel.RefreshAsync();
        Assert.NotNull(viewModel.StatusMessage);

        await viewModel.RefreshAsync();
        Assert.Null(viewModel.StatusMessage);
        Assert.Equal("40.0 °C", CpuRow(viewModel).DisplayValue);
    }

    // FR5.2: with no driver the library can return sensors with no values, rather than throwing.
    [Fact]
    public async Task RefreshAsync_TreatsNoUsableValuesAsAFailure()
    {
        var noValues = new[] { new RawSensor(HardwareKind.Cpu, SensorKind.Temperature, "Core (Tctl/Tdie)", null) };
        var viewModel = CreateViewModel(new FakeSensorSource(noValues));

        await viewModel.RefreshAsync();

        Assert.Equal(WidgetViewModel.UnavailableMessage, viewModel.StatusMessage);
    }

    [Fact]
    public async Task RefreshAsync_LogsOnlyTheFirstOfARunOfFailures()
    {
        var viewModel = CreateViewModel(new FakeSensorSource(
            new InvalidOperationException("first"),
            new InvalidOperationException("second")));

        await viewModel.RefreshAsync();
        await viewModel.RefreshAsync();

        var log = File.ReadAllText(LogPath);
        Assert.Contains("first", log);
        Assert.DoesNotContain("second", log);
    }

    [Fact]
    public async Task RefreshAsync_SurvivesALogThatCannotBeWritten()
    {
        // The log path is an existing folder, so writing to it as a file fails.
        Directory.CreateDirectory(LogPath);
        var viewModel = CreateViewModel(new FakeSensorSource(new InvalidOperationException("driver failed")));

        await viewModel.RefreshAsync();

        Assert.Equal(WidgetViewModel.UnavailableMessage, viewModel.StatusMessage);
    }

    // NFR2: a slow read is not overlapped by the next timer tick.
    [Fact]
    public async Task RefreshAsync_SkipsWhileAnEarlierReadIsRunning()
    {
        using var release = new ManualResetEventSlim(false);
        var source = new BlockingSensorSource(release, CpuAt(40f));
        var viewModel = CreateViewModel(source);

        var firstRefresh = viewModel.RefreshAsync();
        var overlappingRefresh = viewModel.RefreshAsync();
        var overlapWasSkipped = overlappingRefresh.IsCompleted;
        release.Set();
        await Task.WhenAll(firstRefresh, overlappingRefresh);

        Assert.True(overlapWasSkipped);
        Assert.Equal(1, source.ReadCount);
    }

    /// <summary>Plays back each scripted result in turn, repeating the last. An exception in the script is thrown.</summary>
    private sealed class FakeSensorSource : ISensorSource
    {
        private readonly Queue<object> _script;

        public FakeSensorSource(params object[] script) => _script = new Queue<object>(script);

        public IReadOnlyList<RawSensor> ReadSensors()
        {
            var next = _script.Count > 1 ? _script.Dequeue() : _script.Peek();
            return next is Exception failure ? throw failure : (IReadOnlyList<RawSensor>)next;
        }
    }

    private sealed class BlockingSensorSource : ISensorSource
    {
        private readonly ManualResetEventSlim _release;
        private readonly IReadOnlyList<RawSensor> _sensors;
        private int _readCount;

        public BlockingSensorSource(ManualResetEventSlim release, IReadOnlyList<RawSensor> sensors)
        {
            _release = release;
            _sensors = sensors;
        }

        public int ReadCount => Volatile.Read(ref _readCount);

        public IReadOnlyList<RawSensor> ReadSensors()
        {
            Interlocked.Increment(ref _readCount);
            _release.Wait();
            return _sensors;
        }
    }
}
