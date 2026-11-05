using System.Runtime.ExceptionServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Xml.Linq;
using SensorWidget.Core;

namespace SensorWidget.Tests;

/// <summary>
/// FR4.1: no reading has reached a limit on the real PC, so these tests render rows to pixels to show
/// the highlight works. They load the readings list from the real MainWindow.xaml, with its own template
/// and trigger, so a broken trigger fails them. MainWindow itself is never created, because it owns the
/// real sensor source, and nothing here needs sensor hardware or administrator rights.
/// </summary>
public class HighlightRenderingTests
{
    // The window's 340 width less its 1-pixel border and 10-pixel padding on each side.
    private const double RenderWidth = 318;
    private static readonly TimeSpan RenderTimeout = TimeSpan.FromSeconds(60);

    private static readonly XNamespace Presentation = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";

    [Fact]
    public void TooHotRow_ShowsADifferentColorFromANormalRow()
    {
        var pixels = RenderRowBackgrounds(
            new[]
            {
                new RawSensor(HardwareKind.Cpu, SensorKind.Temperature, "Core (Tctl/Tdie)", TemperatureLimits.Cpu),
                new RawSensor(HardwareKind.Gpu, SensorKind.Temperature, "GPU Core", TemperatureLimits.Gpu - 0.1f),
            },
            "CPU Temperature", "GPU Temperature");

        Assert.NotEqual(pixels["GPU Temperature"], pixels["CPU Temperature"]);
        Assert.Equal(WindowBackground(), pixels["GPU Temperature"]);
    }

    // A normal row, a fan and a missing value all keep the window's color, and a renamed row can still turn hot.
    [Fact]
    public void OnlyTooHotRowsChangeColor_OnTheRealMotherboardRows()
    {
        var pixels = RenderRowBackgrounds(
            new[]
            {
                new RawSensor(HardwareKind.Motherboard, SensorKind.Temperature, "Temperature #2", TemperatureLimits.Motherboard),
                new RawSensor(HardwareKind.Motherboard, SensorKind.Temperature, "Temperature #3", TemperatureLimits.Motherboard - 0.1f),
                new RawSensor(HardwareKind.Motherboard, SensorKind.Temperature, "Temperature #5", null),
                new RawSensor(HardwareKind.Motherboard, SensorKind.Fan, "Fan #2", 5000f),
            },
            "Motherboard", "VRM MOSFETs", "AUXTIN2", "CPU Fan");

        var windowColor = WindowBackground();
        Assert.NotEqual(windowColor, pixels["Motherboard"]);
        Assert.Equal(windowColor, pixels["VRM MOSFETs"]);
        Assert.Equal(windowColor, pixels["AUXTIN2"]);
        Assert.Equal(windowColor, pixels["CPU Fan"]);
    }

    private static Color WindowBackground()
    {
        var value = WindowElement().Attribute("Background")?.Value
            ?? throw new InvalidOperationException("MainWindow.xaml sets no Background.");
        return (Color)ColorConverter.ConvertFromString(value);
    }

    private static XElement WindowElement() =>
        XDocument.Load(Path.Combine(ConfigurationAndSecurityTests.AppFolder, "MainWindow.xaml")).Root!;

    /// <summary>The list as the window declares it. It has no event handlers, so it loads without MainWindow.</summary>
    private static string ReadingsListXaml() =>
        WindowElement()
            .Descendants(Presentation + "ItemsControl")
            .Single(element => element.Attribute("ItemsSource")?.Value == "{Binding ReadingGroups}")
            .ToString();

    private static Dictionary<string, Color> RenderRowBackgrounds(
        IReadOnlyCollection<RawSensor> sensors, params string[] labels)
    {
        var groups = ReadingBuilder.Group(ReadingBuilder.Build(sensors));
        var windowColor = WindowBackground();
        var listXaml = ReadingsListXaml();

        return RunOnStaThread(() =>
        {
            var list = (ItemsControl)XamlReader.Parse(listXaml);
            var root = new Border
            {
                Background = new SolidColorBrush(windowColor),
                DataContext = new WindowData(groups),
                Child = list,
            };

            // The list's bindings connect during the first pass, when it still measures as empty,
            // so a second pass is what lays out and draws it at its real height.
            LayOut(root);
            Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ContextIdle);
            LayOut(root);

            var bitmap = new RenderTargetBitmap(
                (int)Math.Ceiling(root.ActualWidth), (int)Math.Ceiling(root.ActualHeight), 96, 96, PixelFormats.Pbgra32);
            bitmap.Render(root);

            return labels.ToDictionary(label => label, label => RowBackgroundPixel(root, bitmap, label));
        });
    }

    private static void LayOut(UIElement root)
    {
        root.Measure(new Size(RenderWidth, double.PositiveInfinity));
        root.Arrange(new Rect(0, 0, RenderWidth, root.DesiredSize.Height));
        root.UpdateLayout();
    }

    /// <summary>Reads the row's left edge, inside its padding, where only its background is drawn.</summary>
    private static Color RowBackgroundPixel(Visual root, BitmapSource bitmap, string label)
    {
        // A group heading can have the same text as a row, but only a row's label sits in a DockPanel.
        var labelText = Assert.Single(
            Descendants(root).OfType<TextBlock>(),
            text => text.Text == label && VisualTreeHelper.GetParent(text) is DockPanel);
        var row = RowOf(labelText);
        var point = row.TransformToAncestor(root).Transform(new Point(1, row.ActualHeight / 2));

        var pixel = new byte[4];
        bitmap.CopyPixels(new Int32Rect((int)point.X, (int)point.Y, 1, 1), pixel, stride: 4, offset: 0);
        return Color.FromArgb(pixel[3], pixel[2], pixel[1], pixel[0]);
    }

    private static Border RowOf(DependencyObject element)
    {
        for (var parent = VisualTreeHelper.GetParent(element); parent is not null; parent = VisualTreeHelper.GetParent(parent))
        {
            if (parent is Border border)
            {
                return border;
            }
        }

        throw new InvalidOperationException("The row's label is not inside a Border.");
    }

    private static IEnumerable<DependencyObject> Descendants(DependencyObject element)
    {
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(element); index++)
        {
            var child = VisualTreeHelper.GetChild(element, index);
            yield return child;

            foreach (var descendant in Descendants(child))
            {
                yield return descendant;
            }
        }
    }

    /// <summary>WPF elements need an STA thread, and test threads are not guaranteed to be one.</summary>
    private static T RunOnStaThread<T>(Func<T> work)
    {
        T result = default!;
        ExceptionDispatchInfo? failure = null;

        var thread = new Thread(() =>
        {
            try
            {
                result = work();
            }
            catch (Exception exception)
            {
                failure = ExceptionDispatchInfo.Capture(exception);
            }
            finally
            {
                Dispatcher.CurrentDispatcher.InvokeShutdown();
            }
        })
        {
            IsBackground = true,
        };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();

        Assert.True(thread.Join(RenderTimeout), "Rendering the readings list did not finish in time.");
        failure?.Throw();
        return result;
    }

    /// <summary>Stands in for the view model. Public because WPF binds only to public types.</summary>
    public sealed record WindowData(IReadOnlyList<ReadingGroup> ReadingGroups);
}
