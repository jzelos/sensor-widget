# Code Summary: Hardware Sensor Widget

## How to Build and Run

- **Prerequisites:** .NET 10 SDK (10.0.403 is installed) and the PawnIO driver, which is not installed yet and is a separate install.
- **Build:** `dotnet build SensorWidget.slnx -c Release`
- **Run:** launch `src\SensorWidget\bin\Release\net10.0-windows\SensorWidget.exe` and accept the Windows permission prompt. Plain `dotnet run` from a non-elevated terminal is likely to fail because the app requires elevation.
- **Tests:** `dotnet test tests/SensorWidget.Tests/SensorWidget.Tests.csproj`. They need no administrator rights, driver or sensor hardware.
- **Failure log:** `%LocalAppData%\SensorWidget\errors.log`

## Files Created

| Path | Purpose |
|------|---------|
| `SensorWidget.slnx` | Solution file |
| `src/.gitignore`, `tests/.gitignore` | Ignore `bin/` and `obj/`; the root `.gitignore` is unchanged |
| `src/SensorWidget.Core/` | Logic with no hardware or UI dependency: `RawSensor`, `ISensorSource`, `TemperatureLimits`, `Reading`, `ReadingBuilder`, `ErrorLog`, `WidgetViewModel` |
| `src/SensorWidget/` | The app: `App.xaml`, `MainWindow.xaml`, `LibreHardwareSensorSource`, `app.manifest` |
| `tests/SensorWidget.Tests/` | `ReadingBuilderTests` (13 cases), `WidgetViewModelTests` (9), `ConfigurationAndSecurityTests` (5) |

## Key Decisions

- **No usable values counts as a failure:** the library may return empty results rather than an error when the driver is missing, and FR5.2's message would otherwise never appear.
- **A failure clears the rows:** old values and highlights cannot pass for current ones. The message is "Readings are unavailable. Trying again every second."
- **Only the first failure in a run is logged:** at one retry per second, logging every failure would grow the file without limit.
- **Sensor fallback:** if the preferred CPU or GPU sensor has no value, the first sensor with a value is used, otherwise `N/A`.
- **One decimal on temperatures:** so that 84.9 °C does not display as 85 without a highlight.

## Fix After the First Real Run

Motherboard rows came through with generic names, so this work returned from Build and Test. By your decisions:

- `Temperature #2` shows as `Motherboard`, `Temperature #3` as `VRM MOSFETs`, `Temperature #5` as `AUXTIN2` and `Fan #2` as `CPU Fan`. These names apply to this board only.
- `Temperature #1` is hidden, because you could not identify it. This is an accepted exception to FR1.4's "every" temperature, and it means an overheat on that sensor would not show.
- Other motherboard fans are hidden while they read exactly 0 RPM. This is an accepted exception to FR1.5, and it means a failed case fan disappears rather than showing 0.
- I chose that `CPU Fan` stays visible at 0 RPM, because a stopped CPU fan should not disappear. A fan with no value still shows `N/A`.
- A new test draws a too-hot row and a normal row through the real template in `MainWindow.xaml` and reads the pixels. It was confirmed to fail when the color trigger was removed or inverted. It reads the XAML source, not the running window.

## Fix After the Second Real Run

By your decisions:

- `CPU Fan` now shows in the CPU group, straight after `CPU Temperature`.
- The GPU Fan row is removed, whatever the GPU reports, because this system is fully watercooled with one set of fans on the CPU header. This changes FR1.3. `requirements.md` still lists FR1.3 as approved, and `traceability.json` records it as not applicable with this reason.

## Test Results

Confirmed by re-running both commands after the fix:

- `dotnet test tests/SensorWidget.Tests/SensorWidget.Tests.csproj`: 55 passed, 0 failed, 0 skipped.
- `dotnet build SensorWidget.slnx -c Release`: 0 warnings, 0 errors.

## Written or Planned Checks Not Yet Run

The fixed build has not been launched. These are left for Build and Test:

- `CPU Fan` under CPU and no GPU Fan row on the real PC (FR1.3, FR1.5)

`LibreHardwareSensorSource` has no automated test, by design, because it touches real hardware.

## Deviations from the Plan

- `App.xaml` and `App.xaml.cs` were created in minimal form at Step 2 rather than Step 7, because the test project references the app and it cannot compile without an entry point.
- Added beyond the plan's named types: `ErrorLog`, `ReadingGroup` and `ReadingBuilder.Group`.
- 27 test cases rather than about 11, including 6 from one limit test. The test instructions and command are unchanged.
- The overlap check that skips a tick is in `WidgetViewModel.RefreshAsync` rather than the timer handler, so it can be tested.
- `StatusMessage` starts as "Reading sensors..." because the library can take a moment to open.

## Risks for Build and Test

- **Partial readings without PawnIO:** this is an expectation, not an observation. GPU readings may work while CPU shows `N/A` and motherboard rows are absent, with no message because some values exist. The library exposes `PawnIo.IsInstalled`, so a hint would be a small addition, but it is not in the plan.
- **Motherboard labels:** rows use the library's own names, which may be generic, such as `Temperature #1`. That may not fully meet FR1's "labelled with what it measures".
- **False motherboard values:** a false reading could set off the 70 °C highlight.
- **Row count (NFR1):** there is no scrolling, so content past 600 pixels would be clipped. About 28 rows are estimated to fit, and the board's real count is unknown.

## Side Effect Outside the Project

The first run of the .NET 10 SDK on this PC installed an ASP.NET Core HTTPS development certificate. It was not marked as trusted, and this work does not use it.
