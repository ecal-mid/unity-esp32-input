#pragma once

#include "BLEDevice.h"
#include "BLEServer.h"
#include "BLEUtils.h"
#include "BLEService.h"
#include "input/InputService.h"

enum ConnectionState
{
  Disconnected,
  Connected
};

class BluetoothConnection : public BLEServerCallbacks
{

public:
  BluetoothConnection();
  void addService(BLEService &service);
  void startAdvertising();
  BLEServer *pServer = NULL;

private:
  ConnectionState connectionState = ConnectionState::Disconnected;
  InputService *inputService = NULL;
  BLEAdvertising *advertising = NULL;

  void onConnect(BLEServer *pServer);
  void onDisconnect(BLEServer *pServer);
};
