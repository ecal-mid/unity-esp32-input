#define LOG_LOCAL_LEVEL ESP_LOG_VERBOSE
#include <Arduino.h>
#include "esp_log.h"
#include "ble/BluetoothConnection.h"
#include "ble/services/BatteryService.h"
#include "ble/services/InputService.h"
#include "io/button.h"
#include "io/motor.h"
#include "io/encoder.h"
#include "io/battery.h"
#include "utils/bleUtils.h"

BluetoothConnection *bluetooth = NULL;
InputService *inputService = NULL;
BatteryService *batteryService = NULL;

BatteryState batteryState = BatteryState();
ButtonState buttonState = ButtonState();
EncoderState encoderState = EncoderState();
MotorState motorState = MotorState();

long lastInput = 0;
int inputSleepTimeoutSeconds = 60 * 5;

void setup()
{

  log_i("**** starting ****");

  initButton(buttonState, GPIO_NUM_27);
  initEncoder(encoderState, 13, 15);
  initMotor(motorState);
  initBattery(batteryState);

  bluetooth = new BluetoothConnection();

  inputService = new InputService(bluetooth->pServer);
  bluetooth->addService(*inputService->service);

  batteryService = new BatteryService(bluetooth->pServer);
  bluetooth->addService(*batteryService->service);

  bluetooth->startAdvertising();
}

void loop()
{
  updateButton(buttonState);
  updateEncoder(encoderState);
  updateMotor(motorState);
  updateBattery(batteryState);

  if (setCharacteristicValueIfChanged(*inputService->buttonCharacteristic, buttonState.isPressed))
  {
    lastInput = millis();
    log_i("is Pressed: %d", buttonState.isPressed);
  }

  if (setCharacteristicValueIfChanged(*inputService->encoderCharacteristic, encoderState.count))
  {
    lastInput = millis();
    log_i("encoder: %d", encoderState.count);
  }
  int batteryLevel = round(batteryState.level * 100);
  if (setCharacteristicValueIfChanged(*batteryService->batteryLevelCharacteristic, batteryLevel))
    log_i("battery level: %d%%", batteryLevel);

  delay(10);

  if (millis() - lastInput > inputSleepTimeoutSeconds * 1000)
  {
    log_i("falling asleep.. no input since more than %is", inputSleepTimeoutSeconds);

    // TODO: Stop the motor in case it hang running
    pinMode(encoderState.encoder.aPinNumber, OUTPUT);
    digitalWrite(encoderState.encoder.aPinNumber, LOW); // turn of the led
    esp_sleep_enable_ext0_wakeup(buttonState.pin, 0);
    esp_deep_sleep_start();
  }
}