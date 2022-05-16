
#define LOG_LOCAL_LEVEL ESP_LOG_VERBOSE
#include <Arduino.h>
#include "BluetoothConnection.h"
#include "BLEDevice.h"
#include "BLEServer.h"
#include "BLEUtils.h"
#include "BLEService.h"
#include "esp_log.h"

BluetoothConnection::BluetoothConnection()
{

  log_i("initializing bluetooth connection");
  BLEDevice::init("ESP32");
  this->pServer = BLEDevice::createServer();
  this->pServer->setCallbacks(this);

  // Start advertising
  this->advertising = BLEDevice::getAdvertising();
}

void BluetoothConnection::addService(BLEService &service)
{
  this->advertising->addServiceUUID(service.getUUID());
}

void BluetoothConnection::startAdvertising()
{
  // pAdvertising->setScanResponse(false);
  // pAdvertising->setMinPreferred(0x0);  // set value to 0x00 to not advertise this parameter
  BLEDevice::startAdvertising();

  log_i("Waiting a client connection to notify...");
}

void BluetoothConnection::onConnect(BLEServer *pServer)
{
  connectionState = ConnectionState::Connected;

  log_i("client connected");
}

void BluetoothConnection::onDisconnect(BLEServer *pServer)
{
  connectionState = ConnectionState::Disconnected;
  pServer->startAdvertising();

  log_i("client disconnected");
}
