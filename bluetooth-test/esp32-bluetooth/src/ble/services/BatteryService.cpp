
#define LOG_LOCAL_LEVEL ESP_LOG_VERBOSE
#include <Arduino.h>
#include "esp_log.h"
#include "BLE2902.h"
#include "BLECharacteristic.h"
#include "BLEService.h"
#include "BatteryService.h"

BatteryService::BatteryService(BLEServer *server)
{
  this->service = server->createService(BATTERY_SERVICE_UUID);

  this->batteryLevelCharacteristic = this->service->createCharacteristic(
      BATTERY_SERVICE_BATTERY_LEVEL_CHARACTERISTIC_UUID,
      BLECharacteristic::PROPERTY_READ | BLECharacteristic::PROPERTY_NOTIFY);
  this->batteryLevelCharacteristic->setCallbacks(this);
  this->batteryLevelCharacteristic->addDescriptor(new BLE2902()); // for notifications

  this->service->start();

  log_i("battery service started");
}

void BatteryService::onRead(BLECharacteristic *pCharacteristic)
{
  log_i("client read characteristic %s", pCharacteristic->getUUID().toString());
}

void BatteryService::onWrite(BLECharacteristic *pCharacteristic)
{
  log_i("client wrote characteristic %s", pCharacteristic->getUUID().toString());
}