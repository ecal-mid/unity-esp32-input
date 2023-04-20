#if UNITY_INPUT_SYSTEM
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.LowLevel;

[ExecuteAlways]
[RequireComponent(typeof(ESP32DeviceManager))]
public class ESP32InputSystemDeviceManager : MonoBehaviour
{
	public Dictionary<ESP32Device, Esp32InputDevice> inputDevices = new Dictionary<ESP32Device, Esp32InputDevice>();

	void OnEnable()
	{
		var esp32DeviceManager = GetComponent<ESP32DeviceManager>();

		for (int i = 0; i < esp32DeviceManager.devices.Count; i++)
		{
			if(esp32DeviceManager.devices[i].connectionState == ESP32Device.ConnectionState.Connected)
				OnDeviceConnected(esp32DeviceManager.devices[i]);
		}

		esp32DeviceManager.OnConnected += OnDeviceConnected;
		esp32DeviceManager.OnDisconnected += OnDeviceDisconnected;
		AddCommandListener();

		
	}

	void OnDisable()
	{
		var esp32DeviceManager = GetComponent<ESP32DeviceManager>();
		esp32DeviceManager.OnConnected -= OnDeviceConnected;
		esp32DeviceManager.OnDisconnected -= OnDeviceDisconnected;

		var devices = new List<ESP32Device>(inputDevices.Keys);
		for (int i = 0; i < devices.Count; i++)
		{
			OnDeviceDisconnected(devices[i]);
		}

		RemoveCommandListener();
	}

	void OnDeviceConnected(ESP32Device device)
	{
		var inputDevice = InputSystem.AddDevice(new InputDeviceDescription
		{
			interfaceName = "ESP32",
			product = "ESP32 Input Device",
			version = "1",
			deviceClass = "ESP32 Input",
			manufacturer = "ecal",
			capabilities = "encoder,button,motor",
			serial = $"{device.sender.address}:{device.sender.port}@{device.receiver.port}",
		}) as Esp32InputDevice;
		InputSystem.SetDeviceUsage(inputDevice,device.name);
		InputSystem.EnableDevice(inputDevice);
		device.OnInputReceived += OnInputReceived;

		inputDevices.Add(device, inputDevice);
	}

	void OnDeviceDisconnected(ESP32Device device)
	{
		InputSystem.RemoveDevice(inputDevices[device]);
		device.OnInputReceived -= OnInputReceived;

		inputDevices.Remove(device);
	}

	void OnInputReceived(ESP32Device device)
	{ 
		InputSystem.QueueStateEvent(inputDevices[device], new Esp32DeviceState
		{
			button = device.currentButtonState.button,
			encoder = device.currentEncoderState.encoder
		});
	}

	unsafe void AddCommandListener()
	{
		InputSystem.onDeviceCommand += OnDeviceCommand;
	}

	unsafe void RemoveCommandListener()
	{
		InputSystem.onDeviceCommand -= OnDeviceCommand;
	}

	private unsafe long? OnDeviceCommand(InputDevice commandDevice, InputDeviceCommand* command)
	{
		if (commandDevice is Esp32InputDevice device)
		{
			if (inputDevices.ContainsValue(device))
			{
				var mainDevice = FindEspDevice(device);
				if (command->type == Esp32HapticEventCommand.Type)
				{
					var cmd = (Esp32HapticEventCommand*)command;
					mainDevice.SendHapticEvent(cmd->motorId, cmd->eventId);
					return 0;
				}

				if (command->type == Esp32HapticRealtimeCommand.Type)
				{
					var cmd = (Esp32HapticRealtimeCommand*)command;
					mainDevice.SendMotorSpeed(cmd->motorId, cmd->speed);
					return 0;
				}
			}
		}


		return null;
	}

	ESP32Device FindEspDevice(Esp32InputDevice device)
	{
		foreach (var pair in inputDevices)
		{
			if (pair.Value == device)
				return pair.Key;
		}

		return null;
	}
}
#endif