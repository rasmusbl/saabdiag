# Connection scenarios

Implemented 2026-11-19: a connection setup screen, explicit local registry inventory and central connection eligibility checks. **Only the simulator establishes a diagnostic session.** This is not a release of real serial, TCP, J2534 or Tech 2 communication.

| Scenario | Setup supported | Communication status |
|---|---|---|
| Offline simulator | Year, simulated response | Available; synthetic fixtures only |
| ELM serial | Explicit registered COM port and documented baud | No serial transport or OBD codec yet |
| ELM TCP | Explicit unicast IP address and TCP port | No socket opened; no TCP transport or codec yet |
| J2534 / MDI candidate | Vendor registration, API version and registry view | No DLL loading, native broker or vehicle compatibility yet |
| Tech 2 vehicle diagnostics | PC serial connection planning | No companion protocol implemented |
| Tech 2 security-access setup | Separate PC-to-scanner connection planning | No authorization provider, codes or scanner communication |

## Using the screen

1. Select the intended scenario in **Connection setup** and a model year.
2. For serial or J2534 routes, explicitly click **Refresh local port / driver list**. This reads registration metadata only. Neither ports nor drivers are selected automatically.
3. Select the port/driver yourself. Supply baud or TCP endpoint from the device documentation; the application assumes no defaults.
4. Read the route, prerequisites and assessment. **Setup complete** is not **Available**. Real modes remain blocked even with complete fields.
5. To run the existing demo, choose **Offline simulator**, then **Connect selected scenario**. The app switches to Diagnostics. Disconnect before changing setup.

Settings are retained only in the current window; they are not saved to disk, exported or restored on startup. No automatic connection, Bluetooth pairing, network scanning, driver installation, vendor configuration launch or fallback to simulation occurs. A rejected connection request invalidates an existing session, so synthetic data cannot appear under a hardware route.

## Local Windows inventory

`WindowsConnectionInventory` reads COM registration strings from `HKLM/HARDWARE/DEVICEMAP/SERIALCOMM` and J2534 registrations from `HKLM/SOFTWARE/PassThruSupport.04.04` and `.05.00` in available 32/64-bit registry views. Only vendor, name and FunctionLibrary metadata are read. DLLs and configuration applications are never executed or opened. Registry view does not prove binary architecture; registration does not prove a device is connected, working, or compatible with Saab.

API 05.00 entries are shown but cannot pass the current 04.04 setup assessment; neither version has an execution broker. Missing/incomplete/ambiguous metadata fails closed. Reads have a three-second UI deadline and cancellation; late results are discarded. An OS registry read already running may finish in the background but cannot open hardware or populate the UI after timeout.

## Architecture and gates

Core `Connectivity` describes routes and assesses settings independently of WPF/Windows registry APIs. `DiagnosticSession.ConnectConfigured` is the execution gate and creates only a simulated transport. All existing diagnostic write restrictions remain in place. Windows inventory is isolated in the desktop project. Unit tests use synthetic inventories, never the real registry or adapters.

Actual communication needs separately implemented transports and evidence-backed profiles. Serial/TCP adapter identity and framing must be reviewed; J2534 needs native process isolation, exact API, DLL architecture and network capabilities; physical Tech 2 needs a reviewed PC protocol and scanner/card/provider identity. Configuration alone never satisfies these gates.

The contact's photograph and statements identify a reported physical Tech 2 with a clone CANdi module, not a verified standalone PC adapter. The two Tech 2 routes are now represented for planning, without changing the primary PC-based replacement goal. Their vehicle and authorization paths are kept separate. The contact's claimed procedure is not hardware validation.

## Sources and provenance

Original implementation; no vendor code, binaries or security algorithms copied. Registry schema research used the manufacturer's [Opus IVS DLL discovery documentation](https://opusivs.com/support/oem-customer-support/program-sae-j2534/discovery-of-the-dll/). API-version separation was checked against [Quantex/ScanDoc's J2534 reference](https://www.scandoc.org/en/develop/j2534/j2534_functions.html). These sources describe discovery conventions, not SaabDiag compatibility or permission to redistribute drivers. Tech 2 workflow context remains in SECURITY_ACCESS.md.
