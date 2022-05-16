#define LOG_LOCAL_LEVEL ESP_LOG_VERBOSE
#include <Arduino.h>
#include "esp_log.h"
#include "motor.h"

void initMotor(MotorState &motorState)
{
    motorState.hasMotor = motorState.drv.begin();

    log_i("has DRV2065", motorState.hasMotor);
}

void updateMotor(MotorState &motorState)
{
}
