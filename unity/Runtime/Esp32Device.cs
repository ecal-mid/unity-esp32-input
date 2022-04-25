using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class Esp32Device : IDisposable
{

	Queue<Esp32Event<Esp32InputState>> oscEventQueue = new Queue<Esp32Event<Esp32InputState>>();
	public List<Esp32InputState> eventsList { get; } = new List<Esp32InputState>();

	public float timeSinceLastEvent { get; private set; }= -1;
	public float timeSinceLastIpSend { get; private set; } = 999;
	public float timeSinceLastHeartbeat { get; private set; }= 999;

	public bool ipAutoSendEnabled => !Application.isEditor;

	public ESP32DeviceInfo deviceInfo { get; private set; }
	public Esp32InputState currentState { get; private set; }


	float ipSendInterval = 5;
	float heartbeatInterval = 5;
	bool firstEncoderValueReceived = false;
	float zeroEncoderValue;

	public Esp32Client client { get; private set; }
	public Esp32Server server { get; private set; }
	public bool IsDisposed { get;private set; }

	public Esp32Device(string address, int port, Esp32Server espServer)
	{
		client = new Esp32Client(address,port);
		
		server = espServer;
		server.OnInfo += OnInfo;
		server.OnInput += OnInput;
	}


	public void Dispose()
	{
		
		if (client != null)
		{
			client.Dispose();
			client = null;
		}

		if (server != null)
		{
			
			server.OnInfo -= OnInfo;
			server.OnInput -= OnInput;
			server = null;
		}

		IsDisposed = true;
	}

	public void Update()
	{
		eventsList.Clear();
		lock (oscEventQueue)
		{
			while (oscEventQueue.Count > 0)
			{
				var evt = oscEventQueue.Dequeue();
				var state = evt.data;

				if (!firstEncoderValueReceived)
				{
					zeroEncoderValue = state.encoder;
					firstEncoderValueReceived = true;
				}

				state.encoder -= zeroEncoderValue;

				eventsList.Add(state);
				currentState = state;

				timeSinceLastEvent = 0;
			}
		}


		if (timeSinceLastEvent >= 0)
			timeSinceLastEvent += Time.deltaTime;

		if (ipAutoSendEnabled)
		{
			timeSinceLastIpSend += Time.deltaTime;
			if (timeSinceLastIpSend > ipSendInterval)
			{
				SendAddress();
				timeSinceLastIpSend = 0;
			}
		}

		timeSinceLastHeartbeat += Time.deltaTime;
		if (timeSinceLastHeartbeat > heartbeatInterval)
		{
			SendHeartbeat();
			timeSinceLastHeartbeat = 0;
		}
	}
	void OnInput(Esp32Event<Esp32InputState> evt)
	{
		if (evt.senderAddress != client.address)
			return;

		lock (oscEventQueue)
		{
			oscEventQueue.Enqueue(evt);
		}
	}

	void OnInfo(Esp32Event<ESP32DeviceInfo> evt)
	{
		if (evt.senderAddress != client.address)
			return;
		
		deviceInfo = evt.data;
		timeSinceLastEvent = 0;
	}


	public void SendMotorSpeed(float speed)
	{
		client.SendMotorSpeed( speed);
	}

	public void SendHapticEvent(int hapticEventId)
	{
		client.SendHapticEvent( hapticEventId);
	}

	public void SendAddress()
	{
		client.SendAddress(server.address,server.port);
	}

	public void SendHeartbeat()
	{
		client.SendHeartbeat();
	}
}