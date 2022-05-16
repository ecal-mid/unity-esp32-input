#pragma once

#include "Adafruit_DRV2605.h"

struct MotorState
{
    Adafruit_DRV2605 drv;
    int8_t motorCurrentMode;
    bool hasMotor = false;
};

void initMotor(MotorState &motorState);

void updateMotor(MotorState &motorState);
