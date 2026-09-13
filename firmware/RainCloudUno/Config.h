#pragma once
// Site calibration is deliberately disabled until clear/overcast measurements exist.
constexpr bool CLOUD_CALIBRATED = false;
constexpr float CLEAR_DELTA_C = -25.0f; // Illustrative only: sky minus sensor ambient
constexpr float OVERCAST_DELTA_C = -5.0f;
constexpr unsigned long SAMPLE_MS = 2000;
constexpr unsigned long STARTUP_MS = 30000;
constexpr unsigned long DRY_HOLD_MS = 300000;
constexpr unsigned long CONTACT_SETTLE_MS = 100;
