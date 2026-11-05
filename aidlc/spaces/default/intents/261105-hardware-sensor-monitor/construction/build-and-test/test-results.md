# Test Results: Hardware Sensor Widget

Status: **passed on the third run**, after two fixes you asked for. See the Loop-Back Log below.

## Build

`dotnet build SensorWidget.slnx -c Release`: succeeded, 0 warnings, 0 errors, on the final code before the widget was launched.

## Unit Tests

`dotnet test tests/SensorWidget.Tests/SensorWidget.Tests.csproj`: 55 total, 55 passed, 0 failed, 0 skipped. No coverage floor applies to this plan.

## Final Run on the Target PC

The final build opened at 340 × 214 pixels with the always-on-top flag set and no `errors.log`. A capture of the window is in `widget-final.png`, and you confirmed the layout is right.

| Check | Result |
|-------|--------|
| Readings at capture | CPU 61.3 °C, CPU Fan 1043 RPM, GPU 32.0 °C, Motherboard 38.0 °C, VRM MOSFETs 37.5 °C, AUXTIN2 48.0 °C |
| CPU use over 2 minutes | Average 0.105% of total, peak 0.26% (24 samples, 5 seconds apart) |
| Memory at about 3 minutes | Task Manager's measure 86.5 MB, full working set 175.1 MB |
| Network | 0 TCP connections and 0 UDP endpoints when checked at about 3 minutes |

The earlier close test showed that clicking the X ends the program: the process had gone when checked, on both occasions it was tried.

## First Real Run on the Target PC

The app was launched with PawnIO 2.2.0.0 installed and running. The widget opened at 340 × 352 pixels with the always-on-top flag set and no `errors.log`. A capture of the window is in `widget-first-run.png`.

| Check | Result | Source |
|-------|--------|--------|
| CPU temperature | "about the same" as another tool (61.5 °C at capture) | Your comparison |
| GPU temperature | "accurate" (30.0 °C at capture) | Your comparison |
| Motherboard rows | Values identified, but labels are generic | Your comparison |
| GPU fan | 0 RPM at capture. Not separately compared | Capture only |
| Stays on top, drags, closes | "All work" | Your check |
| CPU use over 10 minutes | Average 0.112% of total, peak 0.35% (120 samples, 5 seconds apart) | Performance counter |
| Memory at about 11 minutes | Task Manager's measure 104.2 MB, private memory 136.8 MB, full working set 199.1 MB | Performance counter |
| Network | 0 TCP connections and 0 UDP endpoints when checked at the start and the end of the 10 minutes | `Get-NetTCPConnection`, `Get-NetUDPEndpoint` |

The full working set grew from 161.5 MB at 8 seconds to 199.1 MB at about 11 minutes, and was still 199.1 MB at 34 minutes, with Task Manager's measure at 104.1 MB. It levelled off, so there is no sign of a leak over that period.

## Failure Found and Fixed

FR1's "labelled with what it measures" was not met for motherboard rows on the first run: the library has no names for this board, so rows showed `Temperature #1` and `Fan #2`. Both fixes are in the log below.

## Target Verification Matrix

See `build-and-test-summary.md`.

## Loop-Back Log

### Loop-back 1 — 2026-11-05T20:57:16Z

- **Diagnosis:** motherboard rows use the library's generic sensor names, which does not meet FR1.4 and FR1.5.
- **Root-cause stage:** code-generation. `ReadingBuilder` passes the library's names straight through.
- **Planned fix:**
  - Label `Temperature #2` as `Motherboard`, `Temperature #3` as `VRM MOSFETs`, `Temperature #5` as `AUXTIN2` and `Fan #2` as `CPU Fan`.
  - Hide `Temperature #1`, and hide other motherboard fans while they read 0 RPM. The CPU fan stays visible at 0 RPM, because a stopped CPU fan should not disappear.
  - Add a test that the too-hot highlight actually shows on a real window row, because no reading has reached a limit on the real PC.
- **Estimated impact:** effort under an hour; no financial cost; low risk, because the changes are confined to row building and tests. The names apply only to this motherboard, which is the only one targeted.
- **Authorized by:** your answers choosing to give the real names and a code fix.

### Loop-back 2 — 2026-11-05T21:13:58Z

- **Diagnosis:** on the fixed widget (`widget-after-fix.png`), `CPU Fan` appears under Motherboard, and you said it belongs under CPU. You also said this system has no GPU fan, because it is a full watercooled loop with one set of fans on the CPU header, so the GPU Fan row can never show a real value.
- **Root-cause stage:** code-generation. `ReadingBuilder` groups rows by the hardware that reports them and always adds a GPU fan row.
- **Planned fix:** show `CPU Fan` in the CPU group and remove the GPU Fan row. This changes FR1.3 by your decision.
- **Estimated impact:** effort under an hour; no financial cost; low risk, because the changes are confined to row building and its tests.
- **Authorized by:** your feedback on the fixed widget and your choice to remove the row.
