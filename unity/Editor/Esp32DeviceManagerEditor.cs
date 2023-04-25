using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ESP32DeviceManager))]
[CanEditMultipleObjects]
public class Esp32DeviceManagerEditor : Editor
{
	List<int> hapticEvent = new List<int>();
	List<float> motorSpeed = new List<float>();
	ESP32Device activeDevice;

	public override bool RequiresConstantRepaint()
	{
		return true;
	}

	public override void OnInspectorGUI()
	{
		base.OnInspectorGUI();
		var connectionManager = (target as ESP32DeviceManager);

		EditorGUILayout.Space();
		EditorGUILayout.LabelField("Server", EditorStyles.boldLabel);

		EditorGUILayout.LabelField("Local Address", connectionManager.receiver != null ? connectionManager.receiver.address : "none");
		EditorGUILayout.LabelField("Local Port", connectionManager.receiver != null ? connectionManager.receiver.port.ToString() : "none");

		EditorGUILayout.Space();
		EditorGUILayout.LabelField("Devices", EditorStyles.boldLabel);
		if (activeDevice != null && activeDevice.IsDisposed)
			activeDevice = null;

		if (activeDevice == null && connectionManager.devices.Count > 0)
			activeDevice = connectionManager.devices[0];

		EditorGUI.BeginChangeCheck();

		string[] options = new string [connectionManager.devices.Count];
		for (int i = 0; i < connectionManager.devices.Count; i++)
		{
			options[i] = connectionManager.devices[i].name;
		}

		var selectedId = EditorGUILayout.Popup(connectionManager.devices.IndexOf(activeDevice), options);

		if (EditorGUI.EndChangeCheck())
		{
			activeDevice = connectionManager.devices[selectedId];
		}


		if (activeDevice != null)
		{
			EditorGUILayout.Space();
			EditorGUILayout.LabelField("", "Client Connection", EditorStyles.boldLabel);
			EditorGUILayout.LabelField("IP Address", activeDevice.sender.address);
			EditorGUILayout.LabelField("RTT", $"{activeDevice.lastHeartbeatRTT * 1000:0.0}ms");

			GUILayout.BeginHorizontal();
			EditorGUILayout.PrefixLabel("State");
			var prevColor = GUI.color;
			GUI.color = activeDevice.connectionState switch
			{
				ESP32Device.ConnectionState.Connected => new Color(.0f, 1, .0f),
				ESP32Device.ConnectionState.Connecting => new Color(1f, 1f, .0f),
				ESP32Device.ConnectionState.Disconnected => new Color(1f, .0f, .0f),
				_ => throw new ArgumentOutOfRangeException()
			};
			EditorGUILayout.LabelField(activeDevice.connectionState.ToString(), EditorStyles.boldLabel);
			GUILayout.EndHorizontal();
			GUI.color = prevColor;

			GUILayout.BeginHorizontal();
			{
				EditorGUILayout.PrefixLabel(" ");
				GUI.enabled = activeDevice.connectionState == ESP32Device.ConnectionState.Disconnected;
				if (GUILayout.Button("Connect"))
				{
					activeDevice.Connect();
				}

				GUI.enabled = activeDevice.connectionState == ESP32Device.ConnectionState.Connected;
				if (GUILayout.Button("Disconnect"))
				{
					activeDevice.Disconnect();
				}

				GUI.enabled = true;
			}

			GUILayout.EndHorizontal();

			if (activeDevice.connectionState == ESP32Device.ConnectionState.Connected)
			{
				EditorGUILayout.Space();
				EditorGUILayout.LabelField("Device Info", EditorStyles.boldLabel);
				EditorGUILayout.LabelField("Device Name", activeDevice.deviceInfo.name);
				EditorGUILayout.LabelField("Firmware Version", $"{activeDevice.deviceInfo.firmwareVersion}");
				EditorGUILayout.LabelField("Battery Voltage", $"{activeDevice.deviceInfo.batteryVoltage:0.0}V");

				GUILayout.BeginHorizontal();
				{
					EditorGUILayout.PrefixLabel("Battery Level");
					var prevColor2 = GUI.color;
					GUI.color = activeDevice.deviceInfo.batteryLevel > 0.2f ? (activeDevice.deviceInfo.batteryLevel > 0.5f ? new Color(.0f, 1, .0f) : new Color(1f, 1f, .0f)) : new Color(1f, .0f, .0f);
					EditorGUILayout.LabelField($"{Mathf.RoundToInt(activeDevice.deviceInfo.batteryLevel * 100)}%");
					GUI.color = prevColor2;
				}
				GUILayout.EndHorizontal();

				EditorGUILayout.LabelField("Capabilities",
					$"Motors: {activeDevice.deviceInfo.motorCount} Encoders: {activeDevice.deviceInfo.encoderCount} Buttons: {activeDevice.deviceInfo.buttonCount}");
				GUILayout.BeginHorizontal();
				EditorGUILayout.PrefixLabel("Time since last event or heartbeat");
				EditorGUILayout.LabelField(activeDevice.timeSinceLastEvent >= 0 ? $"{activeDevice.timeSinceLastEvent:0.0}s" : "no events received");
				GUILayout.EndHorizontal();

				EditorGUILayout.Space();
				if (activeDevice.deviceInfo.buttonCount > 0 ||
				    activeDevice.deviceInfo.encoderCount > 0)
				{
					EditorGUILayout.LabelField("", "Input", EditorStyles.boldLabel);
					if (activeDevice.deviceInfo.buttonCount > 0)
						EditorGUILayout.LabelField("Button:", activeDevice.currentButtonState.button ? "down" : "up", EditorStyles.boldLabel);
					if (activeDevice.deviceInfo.encoderCount > 0)
						EditorGUILayout.LabelField("Encoder Value:", activeDevice.currentEncoderState.encoder.ToString(), EditorStyles.boldLabel);
					EditorGUILayout.Space();
				}

				if (activeDevice.deviceInfo.motorCount > 0)
				{
					EditorGUILayout.LabelField("", "Output", EditorStyles.boldLabel);
					while(hapticEvent.Count < activeDevice.deviceInfo.motorCount)
						hapticEvent.Add(0);
					while(motorSpeed.Count < activeDevice.deviceInfo.motorCount)
						motorSpeed.Add(0);

					EditorGUILayout.LabelField("", "Motor", EditorStyles.boldLabel);
					for (int i = 0; i < activeDevice.deviceInfo.motorCount; i++)
					{
						GUILayout.BeginHorizontal();
						EditorGUILayout.PrefixLabel("Haptic Event");
						hapticEvent[i] = EditorGUILayout.IntSlider(hapticEvent[i] , 0, 123);
						if (GUILayout.Button("Send", GUILayout.Width(80)))
						{
							activeDevice.SendHapticEvent(i, hapticEvent[i] );
						}

						GUILayout.EndHorizontal();

						GUILayout.BeginHorizontal();
						EditorGUILayout.PrefixLabel("Motor Speed");
						var newMotorSpeed = EditorGUILayout.Slider( motorSpeed[i], 0f, 1f);

						if (Math.Abs(motorSpeed[i] - newMotorSpeed) > 0.00001f)
						{
							activeDevice.SendMotorSpeed(i, newMotorSpeed);
							motorSpeed[i] = newMotorSpeed;
						}
						if (GUILayout.Button("Stop", GUILayout.Width(80)))
						{
							activeDevice.SendMotorSpeed(i, 0);
							motorSpeed[i] = 0;
						}
						GUILayout.EndHorizontal();
					}

					GUILayout.BeginHorizontal();
					EditorGUILayout.PrefixLabel(" ");
					if (GUILayout.Button("Stop all motors"))
					{
						activeDevice.StopMotors();
						motorSpeed.Clear();
					}

					GUILayout.EndHorizontal();
				}

				EditorGUILayout.LabelField("", "Device", EditorStyles.boldLabel);
				GUILayout.BeginHorizontal();
				EditorGUILayout.PrefixLabel("Device", EditorStyles.boldLabel);
				if (GUILayout.Button("Reboot"))
				{
					activeDevice.Reboot();
				}

				if (GUILayout.Button("Sleep"))
				{
					activeDevice.Sleep();
				}

				GUILayout.EndHorizontal();
			}
		}

		UnityEditor.EditorApplication.QueuePlayerLoopUpdate();
	}
}