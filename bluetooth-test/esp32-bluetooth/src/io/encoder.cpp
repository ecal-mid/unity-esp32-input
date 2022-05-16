#define LOG_LOCAL_LEVEL ESP_LOG_VERBOSE
#include <Arduino.h>
#include "esp_log.h"
#include "encoder.h"

void initEncoder(EncoderState &state, int pin1, int pin2)
{

    // ENCODER
    ESP32Encoder::useInternalWeakPullResistors = UP;

    state.encoder = ESP32Encoder();
    state.encoder.attachHalfQuad(pin1, pin2);
    state.encoder.setFilter(1023);
    state.encoder.setCount(0); // reset the counter
}

void updateEncoder(EncoderState &state)
{
    state.prevCount = state.count;
    state.count = state.encoder.getCount();
}

bool hasEncoderChanged(EncoderState &state)
{
    return state.prevCount != state.count;
}