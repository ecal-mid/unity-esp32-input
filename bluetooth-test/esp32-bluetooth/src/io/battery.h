#pragma once
#define LOG_LOCAL_LEVEL ESP_LOG_VERBOSE
#include <Arduino.h>

// Reference voltage on ESP32 is 1.1V
// https://docs.espressif.com/projects/esp-idf/en/latest/esp32/api-reference/peripherals/adc.html#adc-calibration
// See also: https://bit.ly/2zFzfMT
const float refVoltageADC = 1.1;
const float refVoltageOutput = 3.3;
const float maxVoltage = 4.2;
const float minVoltage = 3.5;

struct BatteryState
{
    float voltages[64];
    size_t voltagesCount;
    float voltage;
    float level = 0;
};

inline float invLerp(const float a, const float b, const float value)
{
    float rawVal = (value - a) / (b - a);
    return min(1.0f, max(0.0f, rawVal));
}

void readBatteryVoltage(BatteryState &state)
{

    uint16_t rawValue = analogRead(A13);

    size_t arrayLength = sizeof(state.voltages) / sizeof(state.voltages[0]);
    state.voltagesCount = min(arrayLength, state.voltagesCount + 1);
    for (int i = state.voltagesCount - 1; i >= 1; --i)
    {
        state.voltages[i] = state.voltages[i - 1];
    }
    state.voltages[0] = rawValue / 4095.0 * 2 * refVoltageADC * refVoltageOutput; // calculate voltage level
}

float getAverage(float array[], size_t count)
{
    float avg = 0;
    for (size_t i = 0; i < count; i++)
    {
        avg += array[i];
    }
    avg /= count;
    return avg;
}

void updateBattery(BatteryState &state)
{
    readBatteryVoltage(state);

    state.voltage = getAverage(state.voltages, state.voltagesCount);
    state.level = invLerp(minVoltage, maxVoltage, state.voltage);
}

void initBattery(BatteryState &state)
{
    state = BatteryState();
    state.level = 0;
    state.voltage = 0;
}
