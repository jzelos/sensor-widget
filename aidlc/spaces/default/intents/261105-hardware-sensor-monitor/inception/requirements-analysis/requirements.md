# Requirements: Hardware Sensor Widget

## Sources

- `[desc]` Initial description: "I want to create a small app that will show the temperatures and fan speeds of my computer including CPU, Motherboard and GPU"
- `[Q1]` App form: small always-on-top widget
- `[Q2]` Extras: highlight a reading when it gets too hot
- `[Q3]` Sensor access: run as administrator
- `[review]` Your change at review: "The app should target .Net 10 given its the latest long term release, if the SDK needs installing, I can do that."
- `[assumption]` A value chosen here that you have not yet confirmed

## Intent Analysis

You want to see how hot your PC is running and how fast its fans are spinning, at a glance, while using other programs. The target PC runs Windows 11 with an AMD Ryzen 9 5900X, an MSI MAG X570S TOMAHAWK MAX WIFI motherboard and an AMD Radeon RX 6900 XT. This is a new, single-user, personal tool.

## Functional Requirements

### FR1: Show temperatures and fan speeds `[desc]`

The widget shall show the following, each labelled with what it measures. Temperatures are in °C and fan speeds in RPM.

- **FR1.1** The CPU temperature.
- **FR1.2** The GPU temperature.
- **FR1.3** The GPU fan speed.
- **FR1.4** Every temperature the motherboard reports.
- **FR1.5** Every fan speed the motherboard reports, including the CPU fan header when one is connected.

Pass when each reading is shown and agrees with HWiNFO or LibreHardwareMonitor, running on the same PC at the same time, to within 3 °C or 100 RPM under a steady load.

### FR2: Keep readings current `[assumption]`

The widget shall refresh every reading at least once every 2 seconds for as long as it is open.

Pass when a reading's displayed value changes within 2 seconds of the sensor value changing.

### FR3: Behave as an always-on-top widget `[Q1]`

- **FR3.1** The widget shall stay above other normal windows when another program has focus.
- **FR3.2** You shall be able to drag the widget to any position on screen.
- **FR3.3** You shall be able to close the widget from the widget itself, which ends the program.

### FR4: Highlight readings that are too hot `[Q2]`

- **FR4.1** A temperature reading at or above its limit shall be shown in a clearly different color from normal readings.
- **FR4.2** The highlight shall clear on the first refresh after the reading falls below its limit.
- **FR4.3** The limits are 85 °C for the CPU, 90 °C for the GPU and 70 °C for motherboard temperatures `[assumption]`.

Fan speeds are never highlighted, because you asked about heat only.

### FR5: Handle missing or failed readings `[assumption]`

- **FR5.1** A sensor that returns no value shall be shown as `N/A` while the other readings keep updating.
- **FR5.2** When sensor access fails completely, for example because the hardware driver cannot load, the widget shall stay open and show a plain message saying that readings are unavailable.

### FR6: Run with administrator rights `[Q3]`

The app shall ask Windows for administrator rights when it starts, and it shall read the CPU, motherboard and GPU sensors itself, with no other monitoring tool running.

Pass when launching it shows the Windows permission prompt, and accepting it shows the readings in FR1 with no other monitoring tool running. If you decline the prompt, the app does not start.

## Non-Functional Requirements

- **NFR1 Size** `[assumption]`: The widget shall be no larger than 360 × 600 pixels at 100% display scaling, with all readings visible and no scrolling on the target PC.
- **NFR2 Resource use** `[assumption]`: Averaged over 10 minutes on the target PC, the app shall use less than 2% of total CPU and less than 150 MB of memory.
- **NFR3 Privacy and safety** `[assumption]`: Because it runs with administrator rights, the app shall make no network connections, accept no outside input and only read sensors. It shall never change fan speeds or any other hardware setting.

## Constraints

- Windows 11 on the PC described above. Other computers and operating systems are not targeted `[desc]`.
- Administrator rights are required at each launch `[Q3]`.
- The app targets .NET 10 `[review]`. Only the .NET 8 and 9 SDKs are on this PC now, so you will install the .NET 10 SDK before the build.

## Assumptions

- `[assumption]` The app will be a Windows desktop program using the LibreHardwareMonitor library, which is the common way for a .NET program to read CPU, motherboard and GPU sensors directly.
- `[assumption]` That library can read this motherboard's sensors and its hardware driver can load on this PC. This is the main risk, and the first build will check it.
- `[assumption]` The temperature limits in FR4.3, the 2-second refresh, the size in NFR1 and the resource limits in NFR2 are sensible starting values. Tell me if you want different ones.

## Out of Scope

- Reading history, graphs and logging to a file
- Sounds, pop-ups or other alerts beyond the highlight
- Controlling fan speeds or any other hardware setting
- A settings screen, including changing temperature limits in the app
- Starting automatically with Windows
- An installer
- GPUs, motherboards or operating systems other than those on this PC

## Open Questions

None.
