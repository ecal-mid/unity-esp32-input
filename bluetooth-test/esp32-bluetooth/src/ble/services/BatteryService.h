#pragma once

#include "BLECharacteristic.h"
#include "BLEService.h"

#define BATTERY_SERVICE_UUID "180F"
#define BATTERY_SERVICE_BATTERY_LEVEL_CHARACTERISTIC_UUID "2A19"

class BatteryService : public BLECharacteristicCallbacks
{

public:
  BatteryService(BLEServer *server);
  BLEService *service = NULL;
  BLECharacteristic *batteryLevelCharacteristic = NULL;

private:
  void onRead(BLECharacteristic *pCharacteristic);
  void onWrite(BLECharacteristic *pCharacteristic);
};
