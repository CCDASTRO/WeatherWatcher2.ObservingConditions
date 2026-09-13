# Protocol v1 and ASCOM integration contract
USB serial 9600 baud, 8N1; newline-delimited JSON. Uno may reset when a host opens USB. `get#` returns identification; `getd#` returns the latest snapshot. Streaming occurs every two seconds. One Windows process must own the port; ASCOM clients share its data.

Fields: v, seq, uptime_ms, sample_age_ms, rain, power_ok, nc_closed, no_closed, ir_ok, sky_c, sensor_ambient_c, delta_c, cloud_calibrated, cloud_estimate_pct. Missing numeric measurements are JSON null, never zero substitutes. Sequence increases per response, not necessarily per new IR acquisition; sample_age_ms distinguishes repeated cached data.

Rain states:
- fault: missing power-present signal or invalid relay combination.
- rain: asserted rain relay, immediate reporting at next frame.
- settling: startup/contact stabilization.
- hold: valid no-rain contacts, but five-minute uninterrupted recovery not yet complete.
- dry: recovery completed; means no recent asserted rain, not proven absence of moisture.

The host must validate JSON, protocol, field types and ranges, reject malformed or incomplete records, and track monotonic receive time. Suggested stale deadline: 10 seconds. Error/identity replies are not samples. Repeated seq/uptime must not refresh freshness. A new serial session or uptime reset clears all recovery state. Handle unsigned 32-bit counter wrap. IR measurement age must also be less than the deadline; a responsive board with frozen samples is not healthy. Invalid readings must replace, not preserve, prior safe readings.

Planned ObservingConditions mapping: SkyTemperature from sky_c; calibrated CloudCover from cloud_estimate_pct, explicitly described as an estimate within the IR field of view. Do not overwrite weather-station air temperature with sensor_ambient_c. Rain activity is a separate wet/rain flag, not a fabricated RainRate in mm/hour. Existing real rainfall measurements remain with the weather source.

Planned SafetyMonitor: existing weather SAFE AND rain=dry AND sensor data healthy/fresh AND calibrated cloud estimate below the configured threshold for a configurable clear recovery interval. No stale-value fallback. Cloud faults and reconnects restart the clear interval. This sketch alone does not implement that combined decision.

WeatherWatcher2 currently has a Boltwood input and shared safety-file path behavior. Integrate a dedicated snapshot reader instead of silently writing a partial or invented Boltwood record into an existing weather feed. The integrated reader is implemented; hardware validation is still required.

Implementation now exists: see ASCOM-INTEGRATION.md. WeatherWatcher owns USB directly and publishes the combined expiring safety result; no separate Windows service is required.
