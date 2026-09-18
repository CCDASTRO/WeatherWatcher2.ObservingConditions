# Pushover cloud advisories

Open WeatherWatcher setup and choose **Cloud notifications...**. Notifications are optional and disabled by default. They use the existing NOAA cloud cache; they do not cause extra NOAA requests, change SAFE/UNSAFE, or command the dome.

## Configure

1. Set up the Pushover application on your phone or other supported device. Obtain your user key from your [Pushover dashboard](https://pushover.net/).
2. [Register an application](https://pushover.net/apps/build) named WeatherWatcher to obtain an application API token. Enter the token and your user or group key in the notification dialog. Enter credentials there, not in chat or source code.
3. Optionally enter a target device name; blank uses the Pushover account's default routing.
4. Enable notifications. Defaults: warn at or above **30%**, clearing at or below **20%**, **30-minute cooldown** between automatic attempts. Clearing notifications default on; NOAA unavailable/recovered notifications default off.
5. Use **Send test notification** to send a clearly labeled test with the entered credentials. This works even before enabling automatic notifications. It sends a real message and does not save settings or simulate a weather event.
6. Click OK in this dialog, then OK in the main WeatherWatcher setup to persist changes. Cancel in the main setup discards the notification changes. Keep ObservingConditions connected with Arduino/NOAA mode enabled and valid observatory coordinates configured.

The token and user/group key are masked in the dialog and encrypted in the ASCOM profile using Windows DPAPI for the current Windows user, like existing Ambient credentials. They are never included in trace messages. Moving to another Windows account requires entering them again.

## Behavior

- A fresh NOAA observation at/above the warning threshold triggers one cloud warning. Starting while already cloudy also produces a warning. Starting clear does not send a clearing message.
- A new observation at/below the clearing threshold permits a clearing notice after a delivered warning. Values between the thresholds retain the previous cloud state. Repeated observations do not retrigger alerts.
- One shared notifier serves all ASCOM clients in the WeatherWatcher process. It prevents simultaneous sends and duplicate client alerts.
- The cooldown applies to all automatic notification attempts, including failures and optional availability messages. Suppressed changes are reevaluated against current data after the cooldown. Obsolete warnings are not queued for later delivery. There are no periodic repeats while conditions stay unchanged.
- Fresh cached NOAA data remains usable through a retrieval failure until its observation timestamp expires. Stale, missing, future-dated or invalid readings cannot produce a cloud warning or clearing notice.
- Optional unavailable alerts wait a full NOAA stale-timeout window during initial acquisition; after a valid observation has been seen, its expiry makes data unavailable. A restored-data notice follows recovery if an unavailable notice was delivered. Availability notices take precedence over cloud notices, share the cooldown and include current cloud details when available.
- Notification state/cooldown is in memory and resets when the process restarts or notification/site settings change. A newly started cloudy session may alert again. Disabling stops future automatic attempts; an already submitted message cannot be recalled.
- Automatic notifications require a connected reader. A send already underway may finish after disconnect. The test button is independent of the connection.

The sender uses normal Pushover priority, respects device quiet hours, sets a 30-minute message lifetime, and times out after 15 seconds. NOAA acquisition UTC, observation age, regional cloud percentage and sampling radius appear in cloud messages. These are advisories, not immediate cloud detection: NOAA observations arrive roughly every ten minutes.

Network requests run on a background task after safety publication. Failures are logged without server response bodies or credentials; another attempt is permitted only after the cooldown, if still relevant. If Internet access is down, Pushover cannot deliver an outage notice until connectivity returns. Neither inability to notify nor NOAA cloud state affects RG-11 protection or existing independent weather checks.

Enable WeatherWatcher trace logging to see accepted/failed automatic delivery attempts. “Accepted” means accepted by the Pushover API, not confirmation that the phone displayed or a person read the notification.

[Official Pushover API documentation](https://pushover.net/api)
