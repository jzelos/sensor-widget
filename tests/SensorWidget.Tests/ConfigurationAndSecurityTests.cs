using System.Globalization;
using System.Xml.Linq;

namespace SensorWidget.Tests;

/// <summary>Checks the source and project files, because these rules are not visible to a logic test.</summary>
public class ConfigurationAndSecurityTests
{
    private static readonly string RepositoryRoot = FindRepositoryRoot();
    internal static readonly string AppFolder = Path.Combine(RepositoryRoot, "src", "SensorWidget");

    private static readonly string[] ScannedExtensions = { ".cs", ".xaml", ".csproj" };

    private static readonly string[] ForbiddenTerms =
    {
        // Network APIs (NFR3).
        "System.Net", "HttpClient", "WebClient", "WebRequest", "Socket", "TcpClient", "UdpClient", "IsNetworkEnabled",
        // The library's fan control calls (NFR3).
        "SetSoftware", "SetDefault",
    };

    private static string FindRepositoryRoot()
    {
        for (var folder = new DirectoryInfo(AppContext.BaseDirectory); folder is not null; folder = folder.Parent)
        {
            if (File.Exists(Path.Combine(folder.FullName, "SensorWidget.slnx")))
            {
                return folder.FullName;
            }
        }

        throw new InvalidOperationException("SensorWidget.slnx was not found above the test output folder.");
    }

    private static XElement LoadWindow() =>
        XDocument.Load(Path.Combine(AppFolder, "MainWindow.xaml")).Root!;

    private static double WindowDimension(string attributeName) =>
        double.Parse(LoadWindow().Attribute(attributeName)!.Value, CultureInfo.InvariantCulture);

    private static List<string> ProductionSourceFiles()
    {
        var sourceRoot = Path.Combine(RepositoryRoot, "src");
        return Directory.EnumerateFiles(sourceRoot, "*", SearchOption.AllDirectories)
            .Where(path => ScannedExtensions.Contains(Path.GetExtension(path)))
            .Where(path => !IsBuildOutput(sourceRoot, path))
            .ToList();
    }

    private static bool IsBuildOutput(string sourceRoot, string path) =>
        Path.GetRelativePath(sourceRoot, path)
            .Split(Path.DirectorySeparatorChar)
            .Any(segment => segment is "bin" or "obj");

    // FR6: Windows asks for administrator rights at launch.
    [Fact]
    public void Manifest_RequestsAdministratorRights()
    {
        var manifest = XDocument.Load(Path.Combine(AppFolder, "app.manifest"));

        var executionLevel = Assert.Single(manifest.Descendants(), element => element.Name.LocalName == "requestedExecutionLevel");

        Assert.Equal("requireAdministrator", executionLevel.Attribute("level")?.Value);
    }

    // FR6: a manifest the project does not use would have no effect.
    [Fact]
    public void Project_AppliesTheManifest()
    {
        var project = XDocument.Load(Path.Combine(AppFolder, "SensorWidget.csproj"));

        var manifestSetting = Assert.Single(project.Descendants("ApplicationManifest"));

        Assert.Equal("app.manifest", manifestSetting.Value);
    }

    // FR3.1: the window stays above other windows.
    [Fact]
    public void Window_IsTopmost()
    {
        Assert.Equal("True", LoadWindow().Attribute("Topmost")?.Value);
    }

    // NFR1: no larger than 360 x 600.
    [Fact]
    public void Window_IsWithinTheSizeLimit()
    {
        Assert.InRange(WindowDimension("Width"), 1, 360);
        Assert.InRange(WindowDimension("MaxHeight"), 1, 600);
        Assert.Equal("Height", LoadWindow().Attribute("SizeToContent")?.Value);
    }

    // NFR3: no network use and no fan control anywhere in the source.
    [Fact]
    public void Source_UsesNoNetworkApisOrFanControl()
    {
        var files = ProductionSourceFiles();
        Assert.Contains(files, path => Path.GetFileName(path) == "LibreHardwareSensorSource.cs");

        var violations =
            from path in files
            let text = File.ReadAllText(path)
            from term in ForbiddenTerms
            where text.Contains(term, StringComparison.OrdinalIgnoreCase)
            select $"{Path.GetRelativePath(RepositoryRoot, path)} uses {term}";

        Assert.Empty(violations);
    }
}
