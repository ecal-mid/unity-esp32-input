using System;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Esp32DeviceManager))]
[CanEditMultipleObjects]
public class Esp32DeviceManagerEditor : Editor
{
	private int hapticEvent;
	private float motorSpeed;
	Esp32Device activeDevice;

	public override bool RequiresConstantRepaint()
	{
		return true;
	}

	public override void OnInspectorGUI()
	{
		base.OnInspectorGUI();
		var connectionManager = (target as Esp32DeviceManager);

		EditorGUILayout.Space();
		EditorGUILayout.LabelField("Server", EditorStyles.boldLabel);
		
		EditorGUILayout.LabelField("Local Address", connectionManager.server != null ? connectionManager.server.address : "none");
		EditorGUILayout.LabelField("Local Port", connectionManager.server != null ? connectionManager.server.port.ToString() : "none");

		EditorGUILayout.Space();
		EditorGUILayout.LabelField("Devices", EditorStyles.boldLabel);
		if ((activeDevice == null || activeDevice.IsDisposed) && connectionManager.devices.Count > 0)
			activeDevice = connectionManager.devices[0];
		
		EditorGUI.BeginChangeCheck();

		string[] options = new string [connectionManager.devices.Count];
		for (int i = 0; i < connectionManager.devices.Count; i++)
		{
			options[i] = $"{connectionManager.devices[i].name} ({connectionManager.devices[i].client.address})";
		}
 
		var selectedId = EditorGUILayout.Popup(connectionManager.devices.IndexOf(activeDevice), options);
 
		if (EditorGUI.EndChangeCheck())
		{
			activeDevice = connectionManager.devices[selectedId];
		}
		
		
		if (activeDevice != null)
		{
			EditorGUILayout.Space();
			EditorGUILayout.LabelField("", "Connection", EditorStyles.boldLabel);

			GUILayout.BeginHorizontal();
			EditorGUILayout.PrefixLabel("State");
			var prevColor = GUI.color;
			GUI.color = activeDevice.connectionState switch
			{
				Esp32Device.ConnectionState.Connected => new Color(.0f, 1, .0f),
				Esp32Device.ConnectionState.Connecting => new Color(1f, 1f, .0f),
				Esp32Device.ConnectionState.Disconnected => new Color(1f, .0f, .0f),
				_ => throw new ArgumentOutOfRangeException()
			};
			EditorGUILayout.LabelField(activeDevice.connectionState.ToString(), EditorStyles.boldLabel);
			GUILayout.EndHorizontal();
			GUI.color = prevColor;

			GUILayout.BeginHorizontal();
			EditorGUILayout.PrefixLabel(" ");
			GUILayout.BeginHorizontal();
			GUI.enabled = activeDevice.connectionState == Esp32Device.ConnectionState.Disconnected;
			if (GUILayout.Button("Connect"))
			{
				activeDevice.Connect();
			}
			GUI.enabled = activeDevice.connectionState == Esp32Device.ConnectionState.Connected;
			if (GUILayout.Button("Disconnect"))
			{
				activeDevice.Disconnect();
			}

			GUI.enabled = true;
			GUILayout.EndHorizontal();

			GUILayout.EndHorizontal();

			if (activeDevice.connectionState == Esp32Device.ConnectionState.Connected)
			{
				EditorGUILayout.Space();
				EditorGUILayout.LabelField("Device Info", EditorStyles.boldLabel);
				EditorGUI.indentLevel++;
				EditorGUILayout.LabelField("Device Name", activeDevice.deviceInfo.name);
				EditorGUILayout.LabelField("Firmware Version", $"{activeDevice.deviceInfo.firmwareVersion}");
				EditorGUILayout.LabelField("Battery Voltage", $"{activeDevice.deviceInfo.batteryVoltage:0.0}V");

				GUILayout.BeginHorizontal();
				{
					EditorGUILayout.PrefixLabel("Battery Level");
					var prevColor2 = GUI.color;
					GUI.color = activeDevice.deviceInfo.batteryLevel > 0.2f ? (activeDevice.deviceInfo.batteryLevel > 0.5f ? new Color(.0f, 1, .0f) : new Color(1f, 1f, .0f)) : new Color(1f, .0f, .0f);
					EditorGUI.indentLevel--;
					EditorGUILayout.LabelField($"{Mathf.RoundToInt(activeDevice.deviceInfo.batteryLevel * 100)}%");
					EditorGUI.indentLevel++;
					GUI.color = prevColor2;
				}
				GUILayout.EndHorizontal();

				EditorGUILayout.LabelField("Has Motor", $"{activeDevice.deviceInfo.hasMotor}");
				EditorGUI.indentLevel--;
			}


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

#if UNITY_INPUT_SYSTEM
			EditorGUILayout.Separator();
			EditorGUILayout.LabelField("Input System ", EditorStyles.boldLabel);
			// EditorGUILayout.LabelField("Device Path", espTarget.inputDevice != null ? espTarget.inputDevice.name : "(no device)",EditorStyles.boldLabel);
#endif
		}

		UnityEditor.EditorApplication.QueuePlayerLoopUpdate();
	}
}