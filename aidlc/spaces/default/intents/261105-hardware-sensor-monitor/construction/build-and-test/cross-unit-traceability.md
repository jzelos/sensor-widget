# Coverage Check: Hardware Sensor Widget

**Verdict: pass, with one finding.** The automatic check lists NFR2 as uncovered, because resource use has no single build target and is measured on the running app. It was measured and met in this stage. See `build-and-test-summary.md`.

This work has no Units, so every row is owned by `code-generation`.

| ID | Covered | Target file or reason |
|----|---------|-----------------------|
| FR1.1 | Yes | `src/SensorWidget.Core/ReadingBuilder.cs` |
| FR1.2 | Yes | `src/SensorWidget.Core/ReadingBuilder.cs` |
| FR1.3 | N/A | Removed by your decision: this system is fully watercooled and has no GPU fan |
| FR1.4 | Yes | `src/SensorWidget.Core/ReadingBuilder.cs` |
| FR1.5 | Yes | `src/SensorWidget.Core/ReadingBuilder.cs` |
| FR2 | Yes | `src/SensorWidget.Core/WidgetViewModel.cs` |
| FR3.1 | Yes | `src/SensorWidget/MainWindow.xaml` |
| FR3.2 | Yes | `src/SensorWidget/MainWindow.xaml.cs` |
| FR3.3 | Yes | `src/SensorWidget/MainWindow.xaml.cs` |
| FR4.1 | Yes | `src/SensorWidget.Core/ReadingBuilder.cs` |
| FR4.2 | Yes | `src/SensorWidget.Core/WidgetViewModel.cs` |
| FR4.3 | Yes | `src/SensorWidget.Core/TemperatureLimits.cs` |
| FR5.1 | Yes | `src/SensorWidget.Core/ReadingBuilder.cs` |
| FR5.2 | Yes | `src/SensorWidget.Core/WidgetViewModel.cs` |
| FR6 | Yes | `src/SensorWidget/app.manifest` |
| NFR1 | Yes | `src/SensorWidget/MainWindow.xaml` |
| NFR2 | No build target | Measured on the running app in `test-results.md` |
| NFR3 | Yes | `tests/SensorWidget.Tests/ConfigurationAndSecurityTests.cs` |
