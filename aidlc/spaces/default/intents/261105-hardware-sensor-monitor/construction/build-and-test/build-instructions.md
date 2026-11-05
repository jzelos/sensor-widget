# Build Instructions: Hardware Sensor Widget

## Prerequisites

- Windows 11
- .NET 10 SDK (10.0.403 is installed)
- PawnIO driver, installed separately, for real CPU and motherboard readings. It is not needed to build or to run the unit tests.

## Environment

There are no environment variables, config files or local services. NuGet packages restore automatically during the build, so the first build needs internet access.

## Build

Run from the project root:

```
dotnet build SensorWidget.slnx -c Release
```

## Verify the Build

- The output ends with `0 Warning(s)` and `0 Error(s)`.
- `src\SensorWidget\bin\Release\net10.0-windows\SensorWidget.exe` exists.

## Run

Launch `src\SensorWidget\bin\Release\net10.0-windows\SensorWidget.exe` and accept the Windows permission prompt. Failures are written to `%LocalAppData%\SensorWidget\errors.log`.

## Troubleshooting

| Problem | Likely cause and fix |
|---------|----------------------|
| `net10.0` target not found | The .NET 10 SDK is missing. Check with `dotnet --list-sdks`. |
| Package restore fails | No access to nuget.org. Check the connection and build again. |
| `dotnet run` fails with an elevation error | The app requires administrator rights. Launch the `.exe` directly, or run from an administrator terminal. |
| Build fails because `SensorWidget.exe` is in use | The widget is still running. Close it and build again. |
| CPU shows `N/A` and motherboard rows are missing | PawnIO is probably not installed. Check with `Get-Service PawnIO`. |
