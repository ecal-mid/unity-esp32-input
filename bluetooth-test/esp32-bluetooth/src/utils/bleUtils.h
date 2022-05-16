#include "BLECharacteristic.h"

bool setCharacteristicValueIfChanged(BLECharacteristic &characteristic, int value)
{
    uint8_t *data = characteristic.getData();

    int dataInt;
    memcpy(&dataInt, data, sizeof data);

    if (data == NULL || dataInt != value)
    {
        characteristic.setValue(value);
        characteristic.notify();
        return true;
    }
    return false;
}