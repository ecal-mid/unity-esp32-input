
#define LOG_LOCAL_LEVEL ESP_LOG_VERBOSE
#include <Arduino.h>
#include "esp_log.h"
#include "BLE2902.h"
#include "BLECharacteristic.h"
#include "BLEService.h"
#include "InputService.h"

InputService::InputService(BLEServer *server)
{
  this->service = server->createService(INPUT_SERVICE_UUID);

  this->encoderCharacteristic = this->service->createCharacteristic(
      INPUT_SERVICE_ENCODER_CHARACTERISTIC_UUID,
      BLECharacteristic::PROPERTY_READ | BLECharacteristic::PROPERTY_NOTIFY);
  this->encoderCharacteristic->setCallbacks(this);
  this->encoderCharacteristic->addDescriptor(new BLE2902()); // for notifications

  this->buttonCharacteristic = this->service->createCharacteristic(
      INPUT_SERVICE_BUTTON_CHARACTERISTIC_UUID,
      BLECharacteristic::PROPERTY_READ | BLECharacteristic::PROPERTY_NOTIFY);
  this->buttonCharacteristic->setCallbacks(this);
  this->buttonCharacteristic->addDescriptor(new BLE2902()); // for notifications

  this->service->start();

  log_i("input service started");
}

void InputService::onRead(BLECharacteristic *pCharacteristic)
{
  log_i("client read characteristic %s", pCharacteristic->getUUID().toString());
}

void InputService::onWrite(BLECharacteristic *pCharacteristic)
{
  log_i("client wrote characteristic %s", pCharacteristic->getUUID().toString());
}