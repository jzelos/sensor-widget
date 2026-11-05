# Requirements Analysis Questions

Request: "I want to create a small app that will show the temperatures and fan speeds of my computer including CPU, Motherboard and GPU"

Hardware found on this PC: AMD Ryzen 9 5900X (CPU), MSI MAG X570S TOMAHAWK MAX WIFI (motherboard), AMD Radeon RX 6900 XT (GPU), Windows 11.

## Q1. [Product owner] What form should the app take?

This decides how the readings are shown and how you open and keep the app around.

A. A normal desktop window you open when you want to check the readings
B. A small always-on-top widget that stays visible while you use other programs
C. A dashboard page in your web browser, served by a small program on this PC
D. A text display in a terminal window
X. Other (please specify)

[Answer]: B

## Q2. [Product owner] What should it do besides showing the current readings?

The request only asks for showing the readings, so anything extra adds to the size of the app.

A. Nothing else: current readings only
B. Also highlight a reading when it gets too hot
C. Also show a short history graph of recent readings
D. Both the too-hot highlight and the history graph
X. Other (please specify)

[Answer]: B

## Q3. [Developer] How should the app get access to the motherboard and CPU sensors?

Windows does not offer motherboard fan and temperature readings to ordinary programs. They sit behind a low-level hardware chip that only a program running as administrator can read, through a small driver. GPU readings are much easier to get.

A. Run as administrator: Windows shows a permission prompt at each launch, and the app can read the CPU, motherboard and GPU itself
B. Run without administrator: no prompt, but motherboard fans and temperatures would be missing, which falls short of your request
C. Read from a monitoring tool you keep running, such as HWiNFO or LibreHardwareMonitor: no prompt for this app, but it only works while that tool is running
X. Other (please specify)

[Answer]: A
