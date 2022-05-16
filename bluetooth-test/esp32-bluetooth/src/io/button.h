#pragma once

struct ButtonState
{
    bool prevIsPressed = false;
    bool isPressed = false;
    int pin = 0;
};

void initButton(ButtonState &state, int pin);
void updateButton(ButtonState &state);
bool hasButtonChanged(ButtonState &state);
