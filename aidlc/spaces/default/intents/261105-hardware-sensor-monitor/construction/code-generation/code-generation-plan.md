# Code Generation Plan: Hardware Sensor Widget

## Summary

- Builds: a small always-on-top Windows widget, on .NET 10 and WPF, showing CPU, GPU and motherboard temperatures and fan speeds with a too-hot highlight
- Touches: new `SensorWidget.slnx`, `src/SensorWidget.Core/`, `src/SensorWidget/`, `tests/SensorWidget.Tests/`
- Tests: about 11 xUnit tests, one per requirement, that run without sensor hardware or administrator rights

## Prerequisites You Install

- **.NET 10 SDK**: required to build. Only the .NET 8 and 9 SDKs are on this PC now.
- **PawnIO driver**: reported to be required for motherboard and CPU sensors by recent LibreHardwareMonitor versions, which replaced the older WinRing0 driver that Microsoft now flags as a security risk. It is installed separately and is not on this PC now. The tests do not need it, but real readings probably do.

The build installs NuGet packages only. It does not install an SDK or a driver.

## Design Choices

- **WPF rather than WinForms or Avalonia:** WPF gives an always-on-top borderless window with simple data binding. WinForms needs more manual layout, and Avalonia's cross-platform support is not needed for one Windows PC.
- **LibreHardwareMonitorLib 0.9.6 from NuGet:** this is the latest stable version, and it targets .NET 10. Reading HWiNFO's shared memory was ruled out because you chose for the app to read sensors itself.
- **Two projects:** `SensorWidget.Core` holds all logic with no hardware or UI dependency, so tests run without administrator rights. `SensorWidget` holds the window, the library adapter and the administrator manifest.
- **Security:** the app runs elevated, so it makes no network connections, takes no outside input and never calls the library's fan control methods.

## Steps

- [x] Step 1: Project structure. Create `SensorWidget.slnx`, `src/SensorWidget.Core/SensorWidget.Core.csproj` (`net10.0`), `src/SensorWidget/SensorWidget.csproj` (`net10.0-windows`, WPF, `WinExe`), and a `.gitignore` for `bin/` and `obj/` inside each of `src/` and `tests/`, leaving the root `.gitignore` unchanged. (FR6)
- [x] Step 2: Test runner. Create `tests/SensorWidget.Tests/SensorWidget.Tests.csproj` (xUnit, `net10.0-windows`, referencing both projects) and confirm the exact command in `unit-test-instructions.md` runs. (all requirements)
- [x] Step 3: Business logic, implement. In `SensorWidget.Core`, add `RawSensor` (hardware kind, sensor kind, name, nullable value), `ISensorSource`, `TemperatureLimits` (85 °C CPU, 90 °C GPU, 70 °C motherboard), `Reading` and `ReadingBuilder`. The builder always produces CPU temperature, GPU temperature and GPU fan rows, preferring the `Core (Tctl/Tdie)` CPU sensor and the `GPU Core` GPU sensor, and adds every motherboard temperature and fan. It shows missing values as `N/A`, formats values as °C and RPM, and sets the too-hot flag on temperatures only. (FR1.1 to FR1.5, FR4.1, FR4.3, FR5.1)
- [x] Step 4: Business logic, write and run its tests. Cover rows for each sensor group, the preferred sensor choice, each limit at and below the threshold, no highlight on fans, and `N/A` for a missing value. (FR1, FR4, FR5.1)
- [x] Step 5: Frontend behavior, implement the view model. In `SensorWidget.Core`, add `WidgetViewModel` with `INotifyPropertyChanged`, a `RefreshAsync` method that reads from `ISensorSource` off the UI thread, an update of the readings list, and a `StatusMessage` that catches any source failure and clears on the next good read. A failure is also written to `%LocalAppData%\SensorWidget\errors.log`, and a failure to write that log is ignored so it cannot close the widget. (FR2, FR4.2, FR5.2)
- [x] Step 6: Frontend behavior, write and run the view model tests. Use a fake `ISensorSource` to cover the update on refresh, the highlight clearing when a value falls below its limit, a failing source keeping the widget alive with a message, and recovery after a failure. (FR2, FR4.2, FR5.2)
- [x] Step 7: Frontend behavior, implement the widget. In `src/SensorWidget/`, add `App.xaml`, `MainWindow.xaml` and its code-behind: borderless, `Topmost="True"`, width 340, `MaxHeight="600"`, height sized to content, grouped CPU, GPU and motherboard rows, red highlight for too-hot rows, drag to move and a close button with automation ID `close-button`. A `DispatcherTimer` refreshes every 1 second and skips a tick while a read is running. (FR1, FR2, FR3.1 to FR3.3, FR4.1, NFR1)
- [x] Step 8: Data access, implement the adapter. Add `LibreHardwareSensorSource` in `src/SensorWidget/`, using `Computer` with only CPU, GPU and motherboard enabled, walking hardware and sub-hardware into `RawSensor` rows and closing `Computer` on exit. Add `app.manifest` with `requireAdministrator`. (FR1, FR6, NFR3)
- [x] Step 9: Configuration and security checks, write and run their tests. Check the source and project files: the manifest requests administrator rights, the window is `Topmost` and within the size limit, and no source file uses network APIs or the library's fan control `SetSoftware` calls. (FR3.1, FR6, NFR1, NFR3)
- [x] Step 10: Build configuration. Build the Release configuration with `dotnet build SensorWidget.slnx -c Release` with no errors. (all requirements)
- [x] Step 11: Documentation and traceability. Record how to run the app, its prerequisites and what was and was not verified on real hardware in `code-summary.md`. (all requirements)
- [x] Step 12: Motherboard labels, implement (your request after the first real run). In `SensorWidget.Core`, show `Temperature #2` as `Motherboard`, `Temperature #3` as `VRM MOSFETs`, `Temperature #5` as `AUXTIN2` and `Fan #2` as `CPU Fan`, matching exact library names. Hide `Temperature #1`. Hide any other motherboard fan while it reads exactly 0 RPM, and show it again when it spins. `CPU Fan` stays visible at 0 RPM, the GPU fan row is unchanged, and any other unmapped sensor keeps its library name. The temperature limits are not changed. (FR1.4, FR1.5)
- [x] Step 13: Motherboard labels, write and run their tests. Cover each renamed row, the hidden temperature, a 0 RPM fan hidden then shown again when spinning, `CPU Fan` staying visible at 0 RPM, and an unmapped sensor keeping its name. Update any existing test the new rules change, without weakening what it checks. (FR1.4, FR1.5)
- [x] Step 14: Too-hot highlight on a real window row, write and run a test. Without sensor hardware or administrator rights, render a too-hot row and a normal row through the same XAML template `MainWindow.xaml` uses, and assert that they show different colors. No reading has reached a limit on the real PC, so this is the check that the color trigger works. Any change needed to make that template testable keeps the window's behavior the same. (FR4.1)
- [x] Step 15: Row placement, implement (your request after the second real run). In `SensorWidget.Core`, show the `CPU Fan` row in the CPU group, after `CPU Temperature`, instead of the Motherboard group, wherever a motherboard sensor maps to that label. Remove the GPU Fan row entirely, whatever the GPU reports, because this system has no GPU fan. This replaces Step 3's always-present GPU fan row and changes FR1.3 by your decision. All other rows and rules are unchanged. (FR1.3, FR1.5)
- [x] Step 16: Row placement, write and run the tests. Cover `CPU Fan` appearing in the CPU group and not in the Motherboard group, `CPU Fan` still visible at 0 RPM, no GPU fan row even when the GPU reports a fan sensor, and the Motherboard group heading still showing the other rows. Update existing tests that expected a GPU fan row so they check its absence, and keep every other check they make. (FR1.3, FR1.5)

## Requirement to Step Map

There are no user stories in this plan, so steps map directly to requirements.

| Requirement | Steps |
|-------------|-------|
| FR1 (FR1.1 to FR1.5) | 3, 4, 7, 8 |
| FR2 | 5, 6, 7 |
| FR3 (FR3.1 to FR3.3) | 7, 9 |
| FR4 (FR4.1 to FR4.3) | 3, 4, 5, 6, 7 |
| FR5 (FR5.1, FR5.2) | 3, 4, 5, 6 |
| FR6 | 1, 8, 9 |
| NFR1 | 7, 9 |
| NFR2 | 7 (1-second refresh that skips overlapping reads). Measured on the real PC in Build and Test |
| NFR3 | 8, 9 |

## Not Covered by Automated Tests

These need the real PC, administrator rights and the driver, so Build and Test checks them by running the app: readings compared with another monitoring tool (FR1), always-on-top and dragging (FR3), the permission prompt (FR6), resource use (NFR2) and the absence of network connections while running (NFR3). If a motherboard sensor reports a false value that sets off the highlight, I will raise it there rather than hide it.

## Testing Contract

```json
{
  "version": 1,
  "methodology": "test-after",
  "source": "org",
  "ordering": "implement each applicable testable layer, then write and run that layer's tests.",
  "scope": "express",
  "test_strategy": "minimal",
  "project_type": "greenfield",
  "applicable_notes": [
    {
      "layer": "org",
      "text": "We treat tests as a first-class deliverable in every Bolt. The specific\nmethodology (TDD, BDD, ATDD, or classic test-after) is affirmed at\npractices-discovery and recorded in `team.md` under this heading with explicit\n`Methodology` and `Ordering` fields; Code Generation resolves those fields\nindependently from coverage, tooling, and scope notes.\n\nWhen no posture has been affirmed, our default per scope is:\n- **Methodology**: test-after\n- **Ordering**: implement each applicable testable layer, then write and run\n  that layer's tests.\n- `mvp`, `enterprise`, `feature`, `infra`, `classic` add an 80% line-coverage\n  floor and CI execution before merge.\n- `bugfix`, `security-patch` add a targeted regression for the specific\n  bug/vulnerability and require the existing suite to remain green.\n- `express` uses the Minimal strategy: requirement-driven unit tests (one per\n  requirement, with a happy-path floor per component); existing tests remain\n  green.\n- `poc`, `refactor`, `workshop` add no extra new-test floor and require the\n  existing suite to remain green.\n\nThe active `Test Strategy` still applies in every scope and determines test\nvolume/types. Scope floors are additive; they never reduce or replace the\nselected strategy.\n\nBuild and Test verifies defined coverage floors and affirmed quality targets;\nthey may not be weakened to make a step pass.\n\nAffirm a stricter posture in `team.md` if the team commits to one."
    }
  ],
  "obligations": {
    "strategy": "minimal",
    "strategy_volume": [
      "One verifiable test per requirement at the narrowest effective level.",
      "At least one happy-path unit test per component.",
      "Unit tests are the default; a bugfix/security scope floor may require an integration or E2E regression when that is the narrowest level that reproduces the defect."
    ],
    "scope_floor": [
      "Keep the existing test suite green.",
      "This scope adds no extra new-test floor beyond the selected test strategy."
    ],
    "combination_rule": "Apply every selected-strategy obligation and every scope-floor obligation; neither replaces the other, and a targeted scope regression may add the narrowest necessary test type beyond the strategy default."
  },
  "plan_profile": {
    "methodology": "test-after",
    "runner_step": "Bootstrap the minimal test runner/configuration and record the exact unit-scoped command.",
    "runner_ready_before_first_test": true,
    "testable_layers": [
      "Data model / database behavior",
      "Repository / data access",
      "Business logic",
      "API / endpoint",
      "Frontend behavior"
    ],
    "steps": [
      "Project structure and production configuration skeleton.",
      "Bootstrap the minimal test runner/configuration and record the exact unit-scoped command.",
      "Data model / database behavior - implement.",
      "Data model / database behavior - write and run its tests after implementation.",
      "Repository / data access - implement.",
      "Repository / data access - write and run its tests after implementation.",
      "Business logic - implement.",
      "Business logic - write and run its tests after implementation.",
      "API / endpoint - implement.",
      "API / endpoint - write and run its tests after implementation.",
      "Frontend behavior - implement.",
      "Frontend behavior - write and run its tests after implementation.",
      "Environment/build configuration.",
      "Documentation and traceability."
    ]
  },
  "input_sha256": "sha256:6cb23162168334768ff8f29699669845616670119c9eb58bf9bb05af6fefd3de",
  "contract_sha256": "sha256:283a836cf0757ed2efa4b5dd3135f3986eb59880b3017f4ba87a685cfebedfdb"
}
```
