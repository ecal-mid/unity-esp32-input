using System;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ESP32DeviceManager))]
[CanEditMultipleObjects]
public class Esp32DeviceManagerEditor : Editor
{
	private int hapticEvent;
	private float motorSpeed;
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
			EditorGUILayout.LabelField("RTT", $"{activeDevice.lastHeartbeatRTT*1000:0.0}ms");

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

				EditorGUILayout.LabelField("Has Motor", $"{activeDevice.deviceInfo.hasMotor}");


				EditorGUILayout.Space();
				EditorGUILayout.LabelField("", "Input", EditorStyles.boldLabel);
				EditorGUILayout.LabelField("Button:", activeDevice.currentState.button ? "down" : "up", EditorStyles.boldLabel);
				EditorGUILayout.LabelField("Encoder Value:", activeDevice.currentState.encoder.ToString(), EditorStyles.boldLabel);
				GUILayout.BeginHorizontal();
				EditorGUILayout.PrefixLabel("Time since last event");
				EditorGUILayout.LabelField(activeDevice.timeSinceLastEvent >= 0 ? $"{activeDevice.timeSinceLastEvent:0.0}s" : "no events received");
				GUILayout.EndHorizontal();
				EditorGUILayout.Space();

				EditorGUILayout.LabelField("", "Output", EditorStyles.boldLabel);
				hapticEvent = EditorGUILayout.IntSlider("Haptic Event", hapticEvent, 0, 123);

				GUILayout.BeginHorizontal();
				EditorGUILayout.PrefixLabel(" ");
				if (GUILayout.Button("Send Haptic Event"))
				{
					activeDevice.SendHapticEvent(hapticEvent);
				}

				GUILayout.EndHorizontal();

				var newMotorSpeed = EditorGUILayout.Slider("Motor Speed", motorSpeed, 0f, 1f);

				if (Math.Abs(motorSpeed - newMotorSpeed) > 0.00001f)
				{
					activeDevice.SendMotorSpeed(motorSpeed);
					motorSpeed = newMotorSpeed;
				}

				GUILayout.BeginHorizontal();
				EditorGUILayout.PrefixLabel(" ");
				if (GUILayout.Button("Stop Motor"))
				{
					activeDevice.SendMotorSpeed(0);
					motorSpeed = 0;
				}

				GUILayout.EndHorizontal();

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