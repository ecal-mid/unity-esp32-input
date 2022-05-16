#define LOG_LOCAL_LEVEL ESP_LOG_VERBOSE
#include <Arduino.h>
#include "esp_log.h"
#include "button.h"

static const char *LOG = "BUTTON";

void initButton(ButtonState &state, gpio_num_t pin)
{
    state.pin = pin;
    pinMode(state.pin, INPUT_PULLUP);
}

void updateButton(ButtonState &state)
{
    state.isPressed = digitalRead(state.pin) == 0;
}