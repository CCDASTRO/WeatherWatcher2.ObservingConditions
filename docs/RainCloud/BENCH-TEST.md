# Bench acceptance checks — no observatory movement

1. Compile for Uno R3 using Arduino AVR Boards; no IR, Wire or Adafruit libraries should be required.
2. At 9600 baud, verify valid rain-only JSON every two seconds and on rain-state changes. Check `get#` and `getd#`. Startup must not be dry for the first 30 seconds.
3. Connect the power monitor and relay. Verify dry contacts start the configured Windows dry-out countdown; the Arduino has no second five-minute hold.
4. Wet-test RG-11 according to its manual. Verify an immediate `rain` report, UNSAFE safety output and reset of the dry-out countdown. Repeat during recovery.
5. Remove RG-11 power even while its relay rests dry: UNSAFE. Test COM, NC, NO and power-monitor signal disconnections in both relay states. Record the inactive-contact-wire limitation described in HARDWARE.md.
6. Disconnect USB and stop the WeatherWatcher process separately. Verify serial and safety-file timeouts produce UNSAFE. Reconnect requires recovery.
7. Confirm removing the IR module has no effect on rain protection. SkyTemperature must report not implemented; RainRate must not be invented from relay state.
8. Configure actual observatory coordinates. Verify CloudCover and the NOAA observation timestamp against the downloaded mask. Check overcast, missing coordinates, Internet disconnection, server failure and stale cloud data: none may veto a dry result by itself. Existing separately configured weather-source failures/limits retain their behavior.
9. Send oversized/unknown serial commands and continuous input; rain sampling must continue. No commands change outputs or timing settings.
10. In a controlled automation test, verify UNSAFE prohibits opening a closed dome and causes existing automation to close an open dome. Include rain during a pending NOAA request. Measure total latency including the client's SafetyMonitor polling and physical dome travel.
11. Run an outdoor comparison trial alongside existing protection before deploying unattended.

Automated parser, NOAA, ASCOM and compilation checks do not replace wet testing, wiring verification or end-to-end dome testing. No physical hardware or dome test is claimed by this change.
