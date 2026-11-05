# Unit Test Instructions: Hardware Sensor Widget

## Framework and Setup

- xUnit on .NET 10, in `tests/SensorWidget.Tests/SensorWidget.Tests.csproj`.
- Requires the .NET 10 SDK. It does not need administrator rights, the PawnIO driver or sensor hardware.

## How to Run

Run from the project root:

```
dotnet test tests/SensorWidget.Tests/SensorWidget.Tests.csproj
```

This project has a single test project, so this command covers exactly this work.

## Test Scope (Minimal Strategy)

About 11 tests, one per requirement, with at least one happy-path test per component.

| Test area | Covers |
|-----------|--------|
| `ReadingBuilder` rows for CPU, GPU and every motherboard sensor | FR1.1 to FR1.5 |
| `WidgetViewModel` updates readings on refresh | FR2 |
| Window file is `Topmost` | FR3.1 |
| Too-hot flag at and below each limit, and never on fans | FR4.1, FR4.3 |
| Highlight clears when a value falls below its limit | FR4.2 |
| Missing value shows `N/A` while other rows still show values | FR5.1 |
| Failing source keeps the view model alive with a message, then recovers | FR5.2 |
| Manifest requests `requireAdministrator` | FR6 |
| Motherboard rows use the real names, with a hidden temperature and 0 RPM fans hidden except `CPU Fan` | FR1.4, FR1.5 |
| `CPU Fan` shows in the CPU group, and there is no GPU fan row | FR1.3, FR1.5 |
| A too-hot row and a normal row show different colors through the window's XAML template | FR4.1 |
| Window width and maximum height are within 360 × 600 | NFR1 |
| Source files use no network APIs or fan control calls | NFR3 |

FR3.2, FR3.3 and NFR2 cannot be checked by a unit test. They are checked by running the app in Build and Test.

## Expected Coverage Targets

The express plan sets no line-coverage floor. Every requirement above must have a passing test.

## Mocking and Stubbing

- `WidgetViewModel` tests use a small hand-written fake of `ISensorSource` that returns set values or throws. No mocking library is needed.
- LibreHardwareMonitor is never loaded in tests that exercise logic, because creating its `Computer` object touches real hardware. `LibreHardwareSensorSource` is checked by running the app.

## Test Data

`RawSensor` rows are built in each test. Limit tests use values exactly at the limit, and 0.1 °C below it. Source and file checks read from the repository by walking up from the test output folder to `SensorWidget.slnx`.
