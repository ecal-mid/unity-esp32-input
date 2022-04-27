using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[DefaultExecutionOrder(-12)]
[ExecuteAlways]
public class ESP32DeviceManager : MonoBehaviour
{
	public ESP32InputSettings settings;

	public ESP32Receiver receiver { get; private set; }
	public List<ESP32Device> devices { get; private set; } = new List<ESP32Device>();

	public event Action<ESP32Device> OnConnected;
	public event Action<ESP32Device> OnDisconnected;

	State state = State.NotStarted;

	enum State
	{
		NotStarted,
		Initialized
	}

	void OnEnable()
	{
		Init();
#if UNITY_INPUT_SYSTEM
		InputSystem.onBeforeUpdate += DoUpdate;
#endif
	}

	void OnDisable()
	{
		Cleanup();
#if UNITY_INPUT_SYSTEM
		InputSystem.onBeforeUpdate -= DoUpdate;
#endif
	}

	void OnApplicationPause(bool paused)
	{
		if (paused)
			Cleanup();
		else
			Init();
	}

	void Update()
	{
#if !UNITY_INPUT_SYSTEM
		DoUpdate();
#endif
	}

	void DoUpdate()
	{
		if (state == State.Initialized)
		{
			receiver.SendAllEvents();
			for (int i = 0; i < devices.Count; i++)
			{
				devices[i].Update();
			}
		}
	}

	void Init()
	{
		if (!enabled)
			return;

		if (state != State.NotStarted)
			return;

		if (!settings)
			return;

		try
		{
			receiver = new ESP32Receiver(settings.serverPort);

			for (int i = 0; i < settings.clients.Count; i++)
			{
				var clientSettings = settings.clients[i];
				try
				{
					AddDevice(clientSettings);
				}
				catch (Exception e)
				{
					Debug.LogWarning($"Can't add ESP32 device {clientSettings.address}:{clientSettings.port}\n({e})");
				}
			}

			state = State.Initialized;
			
			LogInfo($"ESP32 input initialized");
		}
		catch (Exception e)
		{
			Cleanup();
			Debug.LogError($"Error initializing ESP32 input: {e}");
		}
		
	}

	void Cleanup()
	{
		if(state != State.Initialized)
			return;

		try
		{

			for (int i = devices.Count - 1; i >= 0; i--)
			{
				RemoveDevice(devices[i]);
			}

			if (receiver != null)
			{
				receiver.Dispose();
				receiver = null;
			}

			state = State.NotStarted;
			LogInfo($"ESP32 input stopped");

		}
		catch (Exception e)
		{
			Debug.Log($"ESP32 input failed to stop: {e}");
		}
	}

	public void Restart()
	{
		Cleanup();
		Init();
	}

	void AddDevice(ESP32ClientSettings settings)
	{
		var device = new ESP32Device(settings, receiver);
		device.OnConnected += OnDeviceConnected;
		device.OnDisconnected += OnDeviceDisconnected;
		devices.Add(device);
	}

	void RemoveDevice(ESP32Device device)
	{
		device.Dispose();
		device.OnConnected -= OnDeviceConnected;
		device.OnDisconnected -= OnDeviceDisconnected;
		devices.Remove(device);
	}

	void OnDeviceConnected(ESP32Device device)
	{
		OnConnected?.Invoke(device);
	}

	void OnDeviceDisconnected(ESP32Device device)
	{
		OnDisconnected?.Invoke(device);
	}

	void LogInfo(string s)
	{
		if(Application.isEditor)
			return;
		Debug.Log(s);
	}
}