# Hardware design

The current hardware is **Uno + Hydreon RG-11 + isolated power monitor**. Remove the GY-906/MLX90614 and its VIN/GND/SDA/SCL wiring completely. A4/A5 are unused. Cloud processing runs on Windows and requires no additional Arduino hardware.

Follow the connection table below. Older `raincloud-wiring.svg`, PNG and Fritzing files depict the retired IR design and are retained only as historical references, not current assembly drawings.

## Parts
Uno R3, USB cable, RG-11, regulated 12 V sensor supply with a fused branch, PC817 optocoupler, 2.2k ohm 1/4 W LED resistor, three 10k pullups, outdoor cable, terminal blocks, sealed enclosure and cable glands. The power monitor below is designed for regulated **12 V**, not the full RG-11 input range. Uno remains USB powered.

## Connections
| Connection | Destination |
|---|---|
| RG-11 PWR1 / PWR2 | Fused +12 V / supply return; confirm terminal markings |
| RG-11 relay COM | Uno GND |
| RG-11 relay NC | Uno D2 |
| RG-11 relay NO | Uno D3 |
| D2, D3, D4 | Each pulled up to Uno 5 V through a separate 10k resistor |

Do not connect sensor 12 V to any Uno input. The dry relay contacts carry only the Uno sensing circuit. No RS485 board is used.

## Sensor-end power monitor
Place a PC817 at the RG-11 supply terminals: +12 V -> 2.2k resistor -> LED anode; LED cathode -> sensor supply return. On the isolated side, collector -> D4 and emitter -> Uno GND. Verify the pinout from the purchased optocoupler's datasheet; do not assume breakout-module pin labels match the bare component. House and insulate the circuit so it does not compromise the sensor seal.

At nominal 12 V the LED current is approximately 5 mA. D4 LOW means supply present. It does NOT prove correct voltage or that the sensor processor is alive. The optical isolation means the 12 V supply return need not be tied to USB ground for this design.

Read both relay throws: NC closed/NO open means no active rain output; NC open/NO closed means rain. Both open or both closed are faults, including transient relay transfer. Loss of the currently inactive contact wire is not detectable until the relay changes state. A short mimicking a valid state or a stuck relay can escape detection. Commission by testing both states and each wire.

## RG-11 configuration and installation
Use Mode 1, “It's Raining.” Verify sensitivity and switch positions against the manual supplied with your unit. Do not use tipping-bucket or dusk-trigger behavior. Keep micro-power mode disabled for normal sensitivity/heater operation. WeatherWatcher adds a configurable dry recovery hold, default five minutes; any sensor-side hold precedes it. The new firmware has no additional dry-out hold. Its modest heater does not melt ice. Mount with an unobstructed view of precipitation and correctly seal its gland/O-ring.

The RG-11 indicates detected water activity, not a direct measurement that a surface has completely dried. Manufacturer instructions and switch tables:
https://rainsensors.com/wp-content/uploads/sites/3/2024/07/rg-11_instructions.pdf
