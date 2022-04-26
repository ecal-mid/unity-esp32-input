using System;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-12)]
[ExecuteAlways]
public class ESP32DeviceManager : MonoBehaviour
{
	public ESP32InputSettings settings;

	public ESP32Receiver receiver { get; private set; }
	public List<ESP32Device> devices { get; private set; } = new List<ESP32Device>();
	
	public event Action<ESP32Device> OnDeviceAdded;
	public event Action<ESP32Device> OnDeviceRemoved;

	State state = State.NotStarted;
	
	enum State
	{
		NotStarted,
		Initialized
	}

	void OnEnable()
	{
		Init();
	}
	
	void OnDisable()
	{
		Cleanup();
	}

	void OnApplicationPause(bool paused)
	{
		if(paused)
			Cleanup();
		else
			Init();
	}
	
	void Update()
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
		if(!enabled)
			return;

		if(state != State.NotStarted)
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
			Debug.Log($"ESP32 input initialized");
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
			Debug.Log($"ESP32 input stopped");

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
		devices.Add(device);
		
		OnDeviceAdded?.Invoke(device);
	}

	void RemoveDevice(ESP32Device device)
	{
		device.Dispose();
		devices.Remove(device);
		
		OnDeviceRemoved?.Invoke(device);
	}

}