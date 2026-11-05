# Build and Test Summary: Hardware Sensor Widget

**Overall status:** passed. The build is clean, all 55 tests pass, and the widget works on the target PC, with your recorded changes to FR1.3, FR1.4 and FR1.5.

## Test Types

- Unit tests: 55 cases, all passing.
- Manual runs on the target PC: readings, window behavior, resource use and network.
- No integration, performance or security instruction files, as the Minimal test strategy generates none.

## Coverage Expectations

This plan sets no line-coverage floor. Every requirement needs a passing test or a recorded check on the real PC.

## Target Verification Matrix

| Target ID | Source | Expected | Actual | Evidence | Owning Stage | Verdict |
|-----------|--------|----------|--------|----------|--------------|---------|
| FR1.1 | requirements.md | CPU temperature, within 3 °C of another tool | "about the same" | Your comparison | build-and-test | Met |
| FR1.2 | requirements.md | GPU temperature, within 3 °C | "accurate" | Your comparison | build-and-test | Met |
| FR1.3 | requirements.md | GPU fan speed | Row removed by your decision: this system has no GPU fan | `widget-final.png`, unit tests | build-and-test | N/A |
| FR1.4 | requirements.md | Every motherboard temperature, labelled | Three labelled by you. `Temperature #1` hidden by your decision | `widget-final.png`, unit tests | build-and-test | Met |
| FR1.5 | requirements.md | Every motherboard fan, labelled | `CPU Fan` labelled and moved to CPU. Other fans hidden at 0 RPM by your decision | `widget-final.png`, unit tests | build-and-test | Met |
| FR2 | requirements.md | Refresh at least every 2 seconds | 1-second timer. Unit tests cover refresh | `test-results.md` | build-and-test | Met |
| FR3.1 | requirements.md | Stays above other windows | Your check passed, always-on-top flag set | Your check | build-and-test | Met |
| FR3.2 | requirements.md | Drags anywhere | Works | Your check | build-and-test | Met |
| FR3.3 | requirements.md | Closes and ends the program | Process gone after the X was clicked, checked twice | `test-results.md` | build-and-test | Met |
| FR4.1 | requirements.md | Too-hot reading in a different color | Render test passes through the real template and fails if the rule is broken | `HighlightRenderingTests` | build-and-test | Met |
| FR4.2 | requirements.md | Highlight clears below the limit | Unit test passes | `test-results.md` | build-and-test | Met |
| FR4.3 | requirements.md | Limits 85, 90 and 70 °C | Unit tests pass at and below each limit | `test-results.md` | build-and-test | Met |
| FR5.1 | requirements.md | Missing sensor shows `N/A` | Unit test passes | `test-results.md` | build-and-test | Met |
| FR5.2 | requirements.md | Total failure keeps the widget open with a message | Unit test passes | `test-results.md` | build-and-test | Met |
| FR6 | requirements.md | Asks for administrator rights, reads sensors without another tool | Launched and read sensors, manifest confirmed | `test-results.md` | build-and-test | Met |
| NFR1 | requirements.md | No larger than 360 × 600, no scrolling | 340 × 214 | Window measurement | build-and-test | Met |
| NFR2 | requirements.md | Under 2% CPU and under 150 MB, by Task Manager's measure as you chose | 0.112% CPU over 10 minutes, 104.2 MB at 11 minutes on the first build. 0.105% over 2 minutes and 86.5 MB on the final build | `test-results.md` | build-and-test | Met |
| NFR3 | requirements.md | No network, read only | 0 connections at every check. Source scan passes | `test-results.md` | build-and-test | Met |
| Testing Contract | code-generation-plan.md | One test per requirement, existing suite green | 55 of 55 pass | `test-results.md` | build-and-test | Met |

## Readiness

- Build-ready: yes.
- Test-ready: yes.
- Deployment-ready: yes, for running on this PC.

## Known Limitations

- The too-hot color has been checked through the real template in a test, but never on the running window, because no reading has reached a limit.
- The full failure message has been tested in code, but not triggered on the real PC.
- A hidden sensor can't show a highlight, so an overheat on `Temperature #1` won't appear. A case fan that fails disappears rather than showing 0.
- The 10-minute resource measurement was on the first build. The final build differs only in row handling and was measured for 2 minutes.
- Network use was checked at set points, not continuously, and this project's source scan doesn't cover the sensor library.
- Motherboard names apply only to this board.
- PawnIO must stay installed for CPU and motherboard readings.
- `requirements.md` still lists FR1.3 and "every" sensor. Your changes are recorded in `code-summary.md` and here.
