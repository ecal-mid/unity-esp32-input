#pragma once

#include "BLECharacteristic.h"
#include "BLEService.h"

#define INPUT_SERVICE_UUID "78880000-d9af-4592-a7ac-4d5830390106"
#define INPUT_SERVICE_ENCODER_CHARACTERISTIC_UUID "78880001-d9af-4592-a7ac-4d5830390106"
#define INPUT_SERVICE_BUTTON_CHARACTERISTIC_UUID "78880002-d9af-4592-a7ac-4d5830390106"

class InputService : public BLECharacteristicCallbacks
{

public:
  InputService(BLEServer *server);
  BLEService *service = NULL;
  BLECharacteristic *encoderCharacteristic = NULL;
  BLECharacteristic *buttonCharacteristic = NULL;

private:
  void onRead(BLECharacteristic *pCharacteristic);
  void onWrite(BLECharacteristic *pCharacteristic);
};
