using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using SensorWidget.Core;

namespace SensorWidget;

public partial class MainWindow : Window
{
    private static readonly TimeSpan RefreshInterval = TimeSpan.FromSeconds(1);

    private readonly LibreHardwareSensorSource _source = new();
    private readonly ErrorLog _errorLog = new(ErrorLog.DefaultPath);
    private readonly WidgetViewModel _viewModel;
    private readonly DispatcherTimer _refreshTimer;

    public MainWindow()
    {
        InitializeComponent();

        _viewModel = new WidgetViewModel(_source, _errorLog);
        DataContext = _viewModel;

        // RefreshAsync skips a tick while an earlier read is still running.
        _refreshTimer = new DispatcherTimer { Interval = RefreshInterval };
        _refreshTimer.Tick += async (_, _) => await _viewModel.RefreshAsync();

        Loaded += OnLoaded;
        Closed += OnClosed;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        _refreshTimer.Start();
        await _viewModel.RefreshAsync();
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        _refreshTimer.Stop();
        try
        {
            _source.Dispose();
        }
        catch (Exception failure)
        {
            // The program is ending either way, so record the failure rather than show a crash at exit.
            _errorLog.Write(failure);
        }
    }

    private void OnWindowMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }

    private void OnCloseButtonClick(object sender, RoutedEventArgs e) => Close();
}
