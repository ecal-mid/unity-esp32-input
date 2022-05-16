#define LOG_LOCAL_LEVEL ESP_LOG_VERBOSE
#include <Arduino.h>
#include "esp_log.h"
#include "ble/BluetoothConnection.h"
#include "io/button.h"
#include "io/motor.h"
#include "io/encoder.h"

BluetoothConnection *bluetooth = NULL;
InputService *inputService = NULL;

ButtonState buttonState = ButtonState();
EncoderState encoderState = EncoderState();
MotorState motorState = MotorState();
bool inputInitialized = false;

void setup()
{

  log_i("init: %d", encoderState.count);

  initButton(buttonState, 27);
  initEncoder(encoderState, 13, 15);
  initMotor(motorState);
  bluetooth = new BluetoothConnection();

  inputService = new InputService(bluetooth->pServer);
  bluetooth->addService(*inputService->service);
  bluetooth->startAdvertising();
}

void loop()
{
  // notify changed value
  // if (deviceConnected) {
  // pCharacteristic->setValue((uint8_t*)&value, 4);
  // pCharacteristic->notify();
  // value++;
  // log_i( "loop");

  updateButton(buttonState);
  updateEncoder(encoderState);
  updateMotor(motorState);

  if (hasButtonChanged(buttonState) || !inputInitialized)
  {
    int btnDown = buttonState.isPressed ? 1 : 0;
    inputService->buttonCharacteristic->setValue(btnDown);
    inputService->buttonCharacteristic->notify();
    log_i("is Pressed: %d", buttonState.isPressed);
  }

  if (hasEncoderChanged(encoderState) || !inputInitialized)
  {
    inputService->encoderCharacteristic->setValue(encoderState.count);
    inputService->encoderCharacteristic->notify();

    log_i("encoder: %d", encoderState.count);
  }

  inputInitialized = true;
  delay(10); // bluetooth stack will go into congestion, if too many packets are sent, in 6 hours test i was able to go as low as 3ms
}
