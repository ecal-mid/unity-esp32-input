using System;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

[DefaultExecutionOrder(-100)]
public class Esp32AutoConnect : MonoBehaviour
{
    public Esp32AutoConnectConfig config;
    public bool overrideHostNameInEditor;
    public string editorOverrideHostName;

    private string currentHostName;

    void Awake()
    {
        currentHostName = overrideHostNameInEditor && Application.isEditor ? editorOverrideHostName : SystemInfo.deviceName;
        
        if (!Application.isEditor)
            Debug.Log($"ESP32-input: Using device name '{SystemInfo.deviceName}' for automatic connection");
    }

    void OnEnable()
    {
        var manager = GetComponent<ESP32DeviceManager>();
        manager.OnDeviceAdded += OnDeviceAdded;
    }

    void OnDisable()
    {
        var manager = GetComponent<ESP32DeviceManager>();
        manager.OnDeviceAdded -= OnDeviceAdded;
    }

    private void OnDeviceAdded(ESP32Device obj)
    {
        if (ShouldAutoConnect(obj.name))
        {
            obj.autoReconnect = true;
            obj.Connect();
        }
    }

    public bool ShouldAutoConnect(string deviceId)
    {
        if (!config)
            return false;

        for (int i = 0; i < config.devices.Count; i++)
        {
            var deviceConfig = config.devices[i];
            try
            {
                var regex = new Regex(deviceConfig.hostname);
                var hostnameMatches = string.IsNullOrEmpty(deviceConfig.hostname) || regex.IsMatch(currentHostName);

                if (hostnameMatches && deviceConfig.deviceId == deviceId)
                {
                    return true;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"There was a problem checking the automatic connection of device '{deviceId}', using regex '{deviceConfig.hostname}': {e}");
            }
        }

        return false;
    }
}