#pragma once
#define LOG_LOCAL_LEVEL ESP_LOG_VERBOSE
#include <Arduino.h>

struct BatteryState
{

    float voltage = 0;
    float level = 0;
};

inline float invLerp(const float a, const float b, const float value)
{
    float rawVal = (value - a) / (b - a);
    return min(1.0f, max(0.0f, rawVal));
}

void updateBattery(BatteryState &state)
{
    // Reference voltage on ESP32 is 1.1V
    // https://docs.espressif.com/projects/esp-idf/en/latest/esp32/api-reference/peripherals/adc.html#adc-calibration
    // See also: https://bit.ly/2zFzfMT
    const float refVoltageADC = 1.1;
    const float refVoltageOutput = 3.3;
    const float maxVoltage = 4.2;
    const float minVoltage = 3.5;
    uint16_t rawValue = analogRead(A13);
    state.voltage = rawValue / 4095.0 * 2 * refVoltageADC * refVoltageOutput; // calculate voltage level
    state.level = invLerp(minVoltage, maxVoltage, state.voltage);
}

void initBattery(BatteryState &state)
{
    state = BatteryState();
    state.level = 0;
    state.voltage = 0;
}
