using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

[CreateAssetMenu()]
public class Esp32AutoConnectConfig : ScriptableObject
{
    [Serializable]
    public class DeviceConfig
    {
        public string hostname;
        public string deviceId;
    }
    
    public List<DeviceConfig> devices;
}