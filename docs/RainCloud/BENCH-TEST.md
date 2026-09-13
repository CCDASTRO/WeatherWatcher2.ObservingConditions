# Bench acceptance checks — no observatory movement
1. Compile for Uno R3 with the actual installed libraries. Confirm Wire timeout support.
2. Open serial at 9600: valid JSON every two seconds; identity and getd commands work. Confirm startup cannot report dry.
3. With power-monitor and relay circuits connected, confirm dry only after startup and five-minute recovery.
4. Wet-test the RG-11 per its manual. Confirm rain promptly; confirm recovery restarts after every rain event.
5. Remove sensor power: fault, even if its relay returns to the dry position. Restore: recovery required.
6. Disconnect COM, NC, NO and power-monitor signal individually. Test NC/NO disconnections in BOTH relay states; document inactive-wire limitations. Both-open/closed must be fault.
7. Disconnect MLX90614 and reconnect: ir_ok false and null temperatures/cloud estimate on failure; no retained clear result. Check stuck SDA/SCL recovers via Wire timeout.
8. Send oversized/unknown commands and continuous bytes; sensor sampling must continue. Commands cannot change outputs or calibration.
9. Disconnect USB: future host reader must become unsafe within its stale timeout. Reconnect resets host recovery.
10. Compare sky/delta readings on multiple clear/overcast nights before enabling cloud calibration. Verify field of view and enclosure thermal influence.
11. Run a prolonged outdoor logging trial alongside independent weather protection before enabling automated use.

Status: design and sketch prepared; hardware not available for these checks. No hardware test results are claimed.
