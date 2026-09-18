# Protocol v1: RG-11 rain telemetry

USB serial remains 9600 baud, 8N1, newline-delimited JSON. `get#` identifies the board and protocol; `getd#` requests a current report. A heartbeat is emitted every two seconds and changes of rain state are emitted immediately. USB opening may reset the Uno. One shared WeatherWatcher session owns the port.

```json
{"v":1,"seq":12,"uptime_ms":45000,"sample_age_ms":0,"rain":"dry","power_ok":true,"nc_closed":true,"no_closed":false}
```

`seq` advances per report. `uptime_ms` is the unsigned board clock. `sample_age_ms` now describes the current rain sample and is zero because contacts are read continuously. The IR fields are no longer emitted; the updated Windows parser also accepts old v1 frames and ignores their IR fields. Old Windows binaries that require IR fields must be upgraded with the firmware.

States:

- `fault`: power monitor absent or both/neither relay contacts closed.
- `rain`: NO contact asserted. No software wet debounce or dry-out delay can postpone UNSAFE.
- `settling`: initial 30-second startup or 100-millisecond contact stabilization.
- `dry`: stable NC contact and sensor power present. Windows still enforces the configured dry-out delay.

Old firmware `hold` frames remain accepted as unsafe. With old firmware, its fixed board hold precedes the Windows hold; update the sketch to use only the configurable Windows delay.

The host validates version, field types, state/contact consistency, unsigned counters and sample age. Duplicate/backward records, reboot, malformed frames, power/contact faults, ten-second serial/sample expiry, and disconnect clear dry recovery. Two distinct sequence numbers may have the same uptime millisecond. Counter wrap is supported conservatively. Identity/error messages are not safety samples.

A dry interval begins only after valid dry frames. Default recovery is 300 seconds (configurable 0–3600 seconds). Any rain, fault, restart or stale gap resets it. A value of zero explicitly disables the extra Windows dry-out hold; board startup and contact stabilization still apply.

The RG-11 relay is a wet/rain flag, not a measurement in mm/hour. Existing weather-station RainRate is preserved. NOAA CloudCover has its own timestamp and cache and is never sent to the Arduino or used for safety. SkyTemperature is not implemented in RG-11 mode.

The existing safety output remains `WW2RC1|UTC ticks|0 or 1` in `C:\ProgramData\WeatherWatcher2\weatherdata.txt`; 0 means combined SAFE and 1 means UNSAFE. The matching SafetyMonitor rejects records ten seconds old or from the future.
