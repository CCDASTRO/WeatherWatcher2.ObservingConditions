#include "Config.h"

// Relay COM -> Uno GND; NC -> D2; NO -> D3. External 10k pullups recommended.
// Power-present optocoupler collector -> D4, emitter -> Uno GND.
constexpr uint8_t NC_PIN = 2, NO_PIN = 3, POWER_PIN = 4;
uint32_t reportAt = 0, contactSince = 0, sequence = 0;
uint8_t lastContacts = 255;
char command[16];
uint8_t commandLength = 0;
bool overflow = false;

// States: fault, rain, settling, dry. No overall SAFE claim is made here.
const char *rainState = "fault";
void updateRain(uint32_t now) {
  bool nc = digitalRead(NC_PIN) == LOW;
  bool no = digitalRead(NO_PIN) == LOW;
  uint8_t contacts = (nc ? 1 : 0) | (no ? 2 : 0);
  if (contacts != lastContacts) { lastContacts = contacts; contactSince = now; }
  if (digitalRead(POWER_PIN) != LOW || nc == no) {
    rainState = "fault"; return;
  }
  if (no) { rainState = "rain"; return; }
  if (now < STARTUP_MS || uint32_t(now - contactSince) < CONTACT_SETTLE_MS) {
    rainState = "settling"; return;
  }
  rainState = "dry";
}

void report(uint32_t now) {
  Serial.print(F("{\"v\":1,\"seq\":")); Serial.print(sequence++);
  Serial.print(F(",\"uptime_ms\":")); Serial.print(now);
  Serial.print(F(",\"sample_age_ms\":")); Serial.print(0);
  Serial.print(F(",\"rain\":\"")); Serial.print(rainState);
  Serial.print(F("\",\"power_ok\":")); Serial.print(digitalRead(POWER_PIN) == LOW ? F("true") : F("false"));
  Serial.print(F(",\"nc_closed\":")); Serial.print(digitalRead(NC_PIN) == LOW ? F("true") : F("false"));
  Serial.print(F(",\"no_closed\":")); Serial.print(digitalRead(NO_PIN) == LOW ? F("true") : F("false"));
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
  reportAt = millis() - SAMPLE_MS;
}
void loop() {
  uint32_t now = millis();
  const char *previous = rainState;
  updateRain(now);
  // A wet/fault transition is sent immediately; the heartbeat also detects cable loss.
  if (strcmp(previous, rainState) || uint32_t(now - reportAt) >= SAMPLE_MS) {
    reportAt = now;
    report(now);
  }
  serialCommands(millis());
}
