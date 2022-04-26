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
			OnDeviceAdded(esp32DeviceManager.devices[i]);
		}

		esp32DeviceManager.OnDeviceAdded += OnDeviceAdded;
		esp32DeviceManager.OnDeviceRemoved += OnDeviceRemoved;
		AddCommandListener();
	}

	void OnDisable()
	{
		var esp32DeviceManager = GetComponent<ESP32DeviceManager>();
		esp32DeviceManager.OnDeviceAdded -= OnDeviceAdded;
		esp32DeviceManager.OnDeviceRemoved -= OnDeviceRemoved;

		var devices = new List<ESP32Device>(inputDevices.Keys);
		for (int i = 0; i < devices.Count; i++)
		{
			OnDeviceRemoved(devices[i]);
		}

		RemoveCommandListener();
	}

	void OnDeviceAdded(ESP32Device device)
	{
		var inputDevice = InputSystem.AddDevice(new InputDeviceDescription
		{
			interfaceName = "ESP32",
			product = "Input Thing",
			version = "1",
			deviceClass = "box",
			manufacturer = "ecal",
			capabilities = "encoder,button,motor",
			serial = $"{device.sender.address}:{device.sender.port}@{device.receiver.port}",
		}) as Esp32InputDevice;
		InputSystem.EnableDevice(inputDevice);

		device.OnInputReceived += OnInputReceived;

		inputDevices.Add(device, inputDevice);
	}

	void OnDeviceRemoved(ESP32Device device)
	{
		device.OnInputReceived -= OnInputReceived;
		
		InputSystem.RemoveDevice(inputDevices[device]);

		inputDevices.Remove(device);
	}

	void OnInputReceived(ESP32Device device, ESP32InputState inputState)
	{
		InputSystem.QueueStateEvent<Esp32DeviceState>(inputDevices[device], inputState);
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
					mainDevice.SendHapticEvent(cmd->eventId);
					return 0;
				}

				if (command->type == Esp32HapticRealtimeCommand.Type)
				{
					var cmd = (Esp32HapticRealtimeCommand*)command;
					mainDevice.SendMotorSpeed(cmd->speed);
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