#if UNITY_INPUT_SYSTEM
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.LowLevel;

[DefaultExecutionOrder(-9)]
public class Esp32InputDeviceManager : MonoBehaviour
{
	public Dictionary<Esp32InputDevice, Esp32Device> inputDevices = new Dictionary<Esp32InputDevice, Esp32Device>();

	void OnEnable()
	{
		var connections = new List<Esp32Device>(FindObjectsOfType<Esp32Device>());
		connections.Sort((a, b) => a.name.CompareTo(b.name));
		for (int i = 0; i < connections.Count; i++)
		{
			var conn = connections[i];
			var inputDevice = InputSystem.AddDevice(new InputDeviceDescription
			{
				interfaceName = "ESP32",
				product = "Input Thing",
				version = "1",
				deviceClass = "box",
				manufacturer = "ecal",
				capabilities = "encoder,button,motor",
				serial = $"{conn.client.address}:{conn.client.port}@{conn.server.port}",
			}) as Esp32InputDevice;
			InputSystem.EnableDevice(inputDevice);

			inputDevices.Add(inputDevice, conn);
		}

		AddCommandListener();
	}

	void OnDisable()
	{

		foreach (var pair in inputDevices)
		{
			InputSystem.RemoveDevice(pair.Key);
		}

		inputDevices.Clear();
		RemoveCommandListener();
	}

	void Update()
	{
		foreach (var pair in inputDevices)
		{
			var device = pair.Key;
			var conn = pair.Value;
			for (var i = 0; i < conn.eventsList.Count; i++)
			{
				InputSystem.QueueStateEvent<Esp32DeviceState>(device, conn.eventsList[i]); 
			}
		}
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
			if (inputDevices.ContainsKey(device))
			{
				var mainDevice = inputDevices[device];
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

}
#endif