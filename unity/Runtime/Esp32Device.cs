using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using UnityEngine;
using OscJack;
using Unity.Collections;
#if UNITY_INPUT_SYSTEM
#endif

[ExecuteAlways]
[DefaultExecutionOrder(-10)]
public class Esp32Device : MonoBehaviour
{
	public string address;
	public int port = 8888;

	Queue<Esp32Event<Esp32InputState>> oscEventQueue = new Queue<Esp32Event<Esp32InputState>>();
	public List<Esp32InputState> eventsList { get; } = new List<Esp32InputState>();

	public float timeSinceLastEvent { get; private set; }
	public float timeSinceLastIpSend { get; private set; }
	public float timeSinceLastHeartbeat { get; private set; }

	public bool ipAutoSendEnabled => !Application.isEditor;

	public ESP32DeviceInfo deviceInfo { get; private set; }
	public Esp32InputState currentState { get; private set; }

	public Esp32OscConnectionManager connectionManager;

	float ipSendInterval = 5;
	float heartbeatInterval = 5;
	bool firstEncoderValueReceived;
	float zeroEncoderValue;

	public Esp32Client client { get; private set; }
	public Esp32Server server { get; private set; }

	void ResetState()
	{
		timeSinceLastEvent = -1;
		timeSinceLastIpSend = 999;
		timeSinceLastHeartbeat = 999;
		firstEncoderValueReceived = false;
	}

	void Awake()
	{
		ResetState();
	}

	void OnEnable()
	{
		client = connectionManager.AddClient(address, port);
		server = connectionManager.server;
		server.OnInfo += OnInfo;
		server.OnInput += OnInput;
	}


	void OnDisable()
	{
		if (client != null)
		{
			connectionManager.RemoveClient(client);
			client = null;
		}

		if (server != null)
		{
			
			server.OnInfo -= OnInfo;
			server.OnInput -= OnInput;
			server = null;
		}
		
		oscEventQueue.Clear();

		ResetState();
	}

	void Update()
	{
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

	void LateUpdate()
	{
		eventsList.Clear();
	}

	void OnInput(Esp32Event<Esp32InputState> evt)
	{
		if (evt.senderAddress != address)
			return;

		lock (oscEventQueue)
		{
			oscEventQueue.Enqueue(evt);
		}
	}

	void OnInfo(Esp32Event<ESP32DeviceInfo> evt)
	{
		if (evt.senderAddress != address)
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