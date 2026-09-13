#include <Wire.h>
#include <Adafruit_MLX90614.h>
#include <math.h>
#include "Config.h"

// Relay COM -> Uno GND; NC -> D2; NO -> D3. External 10k pullups recommended.
// Power-present optocoupler collector -> D4, emitter -> Uno GND.
constexpr uint8_t NC_PIN = 2, NO_PIN = 3, POWER_PIN = 4;
Adafruit_MLX90614 mlx;
bool mlxReady = false, irValid = false, dryTracking = false;
float skyC = NAN, ambientC = NAN;
uint32_t sampleAt = 0, drySince = 0, contactSince = 0, sequence = 0;
uint8_t lastContacts = 255;
char command[16];
uint8_t commandLength = 0;
bool overflow = false;

// States: fault, rain, settling, hold, dry. No overall SAFE claim is made here.
const char *rainState = "fault";
void updateRain(uint32_t now) {
  bool nc = digitalRead(NC_PIN) == LOW;
  bool no = digitalRead(NO_PIN) == LOW;
  uint8_t contacts = (nc ? 1 : 0) | (no ? 2 : 0);
  if (contacts != lastContacts) { lastContacts = contacts; contactSince = now; }
  if (digitalRead(POWER_PIN) != LOW || nc == no) {
    rainState = "fault"; dryTracking = false; return;
  }
  if (no) { rainState = "rain"; dryTracking = false; return; }
  if (now < STARTUP_MS || uint32_t(now - contactSince) < CONTACT_SETTLE_MS) {
    rainState = "settling"; dryTracking = false; return;
  }
  if (!dryTracking) { drySince = now; dryTracking = true; }
  rainState = uint32_t(now - drySince) >= DRY_HOLD_MS ? "dry" : "hold";
}

void readIR() {
  // Retry initialization only during scheduled sampling, never a tight retry loop.
  if (!mlxReady) mlxReady = mlx.begin();
  if (mlxReady) {
    skyC = mlx.readObjectTempC();
    ambientC = mlx.readAmbientTempC();
    irValid = isfinite(skyC) && isfinite(ambientC)
      && skyC >= -70 && skyC <= 100 && ambientC >= -40 && ambientC <= 85;
#if defined(WIRE_HAS_TIMEOUT)
    if (Wire.getWireTimeoutFlag()) { Wire.clearWireTimeoutFlag(); irValid = false; }
#endif
    if (!irValid) mlxReady = false;
  } else irValid = false;
}
void number(float value, bool valid) {
  if (valid && isfinite(value)) Serial.print(value, 2); else Serial.print(F("null"));
}
void report(uint32_t now) {
  bool calibrated = irValid && CLOUD_CALIBRATED && OVERCAST_DELTA_C > CLEAR_DELTA_C;
  float delta = skyC - ambientC;
  float index = constrain(100.0f * (delta - CLEAR_DELTA_C) /
    (OVERCAST_DELTA_C - CLEAR_DELTA_C), 0.0f, 100.0f);
  Serial.print(F("{\"v\":1,\"seq\":")); Serial.print(sequence++);
  Serial.print(F(",\"uptime_ms\":")); Serial.print(now);
  Serial.print(F(",\"sample_age_ms\":")); Serial.print(uint32_t(now - sampleAt));
  Serial.print(F(",\"rain\":\"")); Serial.print(rainState);
  Serial.print(F("\",\"power_ok\":")); Serial.print(digitalRead(POWER_PIN) == LOW ? F("true") : F("false"));
  Serial.print(F(",\"nc_closed\":")); Serial.print(digitalRead(NC_PIN) == LOW ? F("true") : F("false"));
  Serial.print(F(",\"no_closed\":")); Serial.print(digitalRead(NO_PIN) == LOW ? F("true") : F("false"));
  Serial.print(F(",\"ir_ok\":")); Serial.print(irValid ? F("true") : F("false"));
  Serial.print(F(",\"sky_c\":")); number(skyC, irValid);
  Serial.print(F(",\"sensor_ambient_c\":")); number(ambientC, irValid);
  Serial.print(F(",\"delta_c\":")); number(delta, irValid);
  Serial.print(F(",\"cloud_calibrated\":")); Serial.print(calibrated ? F("true") : F("false"));
  Serial.print(F(",\"cloud_estimate_pct\":")); number(index, calibrated);
  Serial.println(F("}"));
}
void serialCommands(uint32_t now) {
  // Bound work per iteration, so continuous incoming traffic cannot starve rain sampling.
  for (uint8_t n = 0; n < 32 && Serial.available(); ++n) {
    char ch = Serial.read();
    if (ch == '#' || ch == '\n') {
      command[commandLength] = '\0';
      if (overflow) Serial.println(F("{\"error\":\"command_too_long\"}"));
      else if (!strcmp(command, "get")) Serial.println(F("{\"device\":\"CCDASTRO RainCloud\",\"protocol\":1}"));
      else if (!strcmp(command, "getd")) report(now);
      else if (commandLength) Serial.println(F("{\"error\":\"unknown_command\"}"));
      commandLength = 0; overflow = false;
    } else if (ch != '\r') {
      if (commandLength < sizeof(command) - 1 && !overflow) command[commandLength++] = ch;
      else overflow = true;
    }
  }
}
void setup() {
  pinMode(NC_PIN, INPUT_PULLUP); pinMode(NO_PIN, INPUT_PULLUP); pinMode(POWER_PIN, INPUT_PULLUP);
  Serial.begin(9600);
  Wire.begin();
#if defined(WIRE_HAS_TIMEOUT)
  Wire.setWireTimeout(25000, true);
#endif
  // No sensor reads until loop; startup is never classified dry.
  sampleAt = millis() - SAMPLE_MS;
}
void loop() {
  uint32_t now = millis();
  updateRain(now);
  if (uint32_t(now - sampleAt) >= SAMPLE_MS) {
    readIR();
    now = millis(); sampleAt = now;
    updateRain(now); report(now);
  }
  serialCommands(millis());
}
