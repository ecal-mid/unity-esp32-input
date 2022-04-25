#if UNITY_INPUT_SYSTEM
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.LowLevel;

[DefaultExecutionOrder(-9)]
public class Esp32DeviceManager : MonoBehaviour
{
	public Dictionary<Esp32Device, Esp32DeviceConnection> inputDevices = new Dictionary<Esp32Device, Esp32DeviceConnection>();

	void OnEnable()
	{
		var connections = new List<Esp32DeviceConnection>(FindObjectsOfType<Esp32DeviceConnection>());
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
				serial = $"{conn.clientAddress}:{conn.clientPort}@{conn.serverPort}",
			}) as Esp32Device;
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
		if (commandDevice is Esp32Device device)
		{
			if (inputDevices.ContainsKey(device))
			{
				var connection = inputDevices[device];
				if (command->type == Esp32HapticEventCommand.Type)
				{
					var cmd = (Esp32HapticEventCommand*)command;
					connection.SendHapticEvent(cmd->eventId);
					return 0;
				}

				if (command->type == Esp32HapticRealtimeCommand.Type)
				{
					var cmd = (Esp32HapticRealtimeCommand*)command;
					connection.SendMotorSpeed(cmd->speed);
					return 0;
				}
			}
		}


		return null;
	}

}
#endif