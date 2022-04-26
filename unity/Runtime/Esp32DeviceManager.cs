using System;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-12)]
[ExecuteAlways]
public class Esp32DeviceManager : MonoBehaviour
{
	public Esp32InputSettings settings;

	public Esp32Server server { get; private set; }
	public List<Esp32Device> devices { get; private set; } = new List<Esp32Device>();
	
	public event Action<Esp32Device> OnDeviceAdded;
	public event Action<Esp32Device> OnDeviceRemoved;

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
	
	void Update()
	{
		server.SendAllEvents();
		for (int i = 0; i < devices.Count; i++)
		{
			devices[i].Update();
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

		server = new Esp32Server(settings.serverPort);

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
	}
	
	void Cleanup()
	{
		if(state != State.Initialized)
			return;
		
		for (int i = devices.Count - 1; i >= 0; i--)
		{
			RemoveDevice(devices[i]);
		}

		if (server != null)
		{
			server.Dispose();
			server = null;
		}
		state = State.NotStarted;
	}

	public void Restart()
	{
		Cleanup();
		Init();
	}

	void AddDevice(Esp32ClientConnectionSettings settings)
	{
		var device = new Esp32Device(settings, server);
		devices.Add(device);
		
		OnDeviceAdded?.Invoke(device);
	}

	void RemoveDevice(Esp32Device device)
	{
		device.Dispose();
		devices.Remove(device);
		
		OnDeviceRemoved?.Invoke(device);
	}

}