#pragma once

#include <ESP32Encoder.h>

struct EncoderState
{
    ESP32Encoder encoder = 0;
    int count = 0;
    int prevCount = 0;
};

void initEncoder(EncoderState &state, int pin1, int pin2);
void updateEncoder(EncoderState &state);
bool hasEncoderChanged(EncoderState &state);