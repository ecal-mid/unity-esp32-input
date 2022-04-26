using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class Esp32Device : IDisposable
{
	public enum ConnectionState
	{
		Disconnected,
		Connecting,
		Connected
	}

	Queue<Esp32Event<Esp32InputState>> oscEventQueue = new Queue<Esp32Event<Esp32InputState>>();
	public List<Esp32InputState> eventsList { get; } = new List<Esp32InputState>();

	public float connectionStateTime { get; private set; } = 0;
	public float timeSinceLastEvent { get; private set; } = -1;
	public float timeSinceLastIpSend { get; private set; } = 999;

	public bool ipAutoSendEnabled => !Application.isEditor;

	public ESP32DeviceInfo deviceInfo { get; private set; }
	public Esp32InputState currentState { get; private set; }

	int heartbeatMsgId;
	float heartbeatSendTime;
	float heartbeatReceiveTime;
	int failedHeartbeats;
	int maxFailedHeartbeatsForDisconnect = 3;
	public float lastHeartbeatRTT { get; private set; }

	enum HeartbeatState
	{
		Idle,
		WaitingForResponse
	}

	HeartbeatState heartbeatState = HeartbeatState.Idle;

	float ipSendInterval = 5;
	float heartbeatInterval = 5;
	bool firstEncoderValueReceived = false;
	float zeroEncoderValue;
	public string name;

	public Esp32Client client { get; private set; }
	public Esp32Server server { get; private set; }
	public bool IsDisposed { get; private set; }

	public ConnectionState connectionState { get; private set; }

	public Esp32Device(Esp32ClientConnectionSettings settings, Esp32Server espServer)
	{
		name = settings.name;
		client = new Esp32Client(settings.address, settings.port);

		server = espServer;
		server.OnInfo += OnInfo;
		server.OnInput += OnInput;
		server.OnDisconnect += OnDisconnect;
		server.OnAlive += OnAlive;
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
			server.OnDisconnect -= OnDisconnect;
			server.OnAlive -= OnAlive;
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


		connectionStateTime += Time.deltaTime;

		if (timeSinceLastEvent >= 0)
			timeSinceLastEvent += Time.deltaTime;

		if (ipAutoSendEnabled)
		{
			timeSinceLastIpSend += Time.deltaTime;
			if (timeSinceLastIpSend > ipSendInterval)
			{
				Connect();
				timeSinceLastIpSend = 0;
			}
		}

		switch (connectionState)
		{
			case ConnectionState.Connected:
			{
				if (Time.time - heartbeatSendTime > heartbeatInterval)
				{
					if (heartbeatState == HeartbeatState.WaitingForResponse) // previous heartbeat didn't get a response
					{
						failedHeartbeats++;
						Debug.LogWarning($"Heartbeat {heartbeatMsgId} didn't get a response ({failedHeartbeats})");
					}

					if (failedHeartbeats >= maxFailedHeartbeatsForDisconnect)
					{
						SetState(ConnectionState.Disconnected);
					}
					else
					{
						SendHeartbeat();
					}
				}

				break;
			}
			case ConnectionState.Connecting:
				if (connectionStateTime > 5) //timeout
					SetState(ConnectionState.Disconnected);

				break;
		}
	}

	void OnInput(Esp32Event<Esp32InputState> evt)
	{
		if (evt.senderAddress != client.address)
			return;

		if (connectionState == ConnectionState.Connected)
		{
			lock (oscEventQueue)
			{
				oscEventQueue.Enqueue(evt);
			}
		}
	}

	void OnDisconnect(Esp32Event<ESP32DisconnectInfo> evt)
	{
		if (evt.senderAddress != client.address)
			return;
		if (connectionState == ConnectionState.Connected)
		{
			SetState(ConnectionState.Disconnected);
		}
	}

	void OnInfo(Esp32Event<ESP32DeviceInfo> evt)
	{
		if (evt.senderAddress != client.address)
			return;
		if (connectionState == ConnectionState.Connected || connectionState == ConnectionState.Connecting)
		{
			deviceInfo = evt.data;
			timeSinceLastEvent = 0;

			SetState(ConnectionState.Connected);
		}
	}

	void OnAlive(Esp32Event<ESP32AliveMessage> evt)
	{
		if (evt.senderAddress != client.address)
			return;

		if (connectionState == ConnectionState.Connected)
		{
			if (heartbeatState == HeartbeatState.WaitingForResponse)
			{
				if (evt.data.msgId == heartbeatMsgId)
				{
					//Debug.Log($"receive heartbeat {evt.data.msgId}");
					heartbeatState = HeartbeatState.Idle;
					heartbeatReceiveTime = Time.time;
					lastHeartbeatRTT = heartbeatReceiveTime - heartbeatSendTime;
					failedHeartbeats = 0;
				}
				else
				{
					//Debug.Log($"receive OUTDATED heartbeat {evt.data.msgId}");
				}
			}
		}
	}


	public void SendMotorSpeed(float speed)
	{
		if (connectionState == ConnectionState.Connected)
			client.SendMotorSpeed(speed);
	}

	public void SendHapticEvent(int hapticEventId)
	{
		if (connectionState == ConnectionState.Connected)
			client.SendHapticEvent(hapticEventId);
	}

	public void Connect()
	{
		if (connectionState == ConnectionState.Disconnected)
		{
			SetState(ConnectionState.Connecting);
			client.Connect(server.address, server.port);
		}
	}


	public void Disconnect()
	{
		if (connectionState == ConnectionState.Connected)
		{
			client.Disconnect();
			SetState(ConnectionState.Disconnected);
		}
	}

	public void SendHeartbeat()
	{
		heartbeatMsgId++;
		client.SendHeartbeat(heartbeatMsgId);
		heartbeatSendTime = Time.time;
		heartbeatState = HeartbeatState.WaitingForResponse;
		//Debug.Log($"send heartbeat {heartbeatMsgId}");
	}

	void SetState(ConnectionState connectionState)
	{
		this.connectionState = connectionState;
		connectionStateTime = 0;

		switch (connectionState)
		{
			case ConnectionState.Disconnected:
				heartbeatState = HeartbeatState.Idle;
				failedHeartbeats = 0;
				currentState = new Esp32InputState
				{
					button = false,
					encoder = 0
				};
				break;
		}
	}

	public void Reboot()
	{
		client.SendReboot();
	}

	public void Sleep()
	{
		client.SendSleep();
	}
}