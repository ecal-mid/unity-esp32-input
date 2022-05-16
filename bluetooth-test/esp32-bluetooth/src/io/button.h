#pragma once

struct ButtonState
{
    bool isPressed = false;
    gpio_num_t pin = GPIO_NUM_0;
};

void initButton(ButtonState &state, gpio_num_t pin);
void updateButton(ButtonState &state);
