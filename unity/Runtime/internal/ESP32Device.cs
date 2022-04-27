using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ESP32Device : IDisposable
{
	public enum ConnectionState
	{
		Disconnected,
		Connecting,
		Connected
	}

	public event Action<ESP32Device, ESP32InputState> OnInputReceived;
	public event Action<ESP32Device> OnConnected;
	public event Action<ESP32Device> OnDisconnected;
	public float connectionStateTime { get; private set; } = 0;
	public float timeSinceLastEvent { get; private set; } = -1;

	public bool autoConnectInBuild { get; private set; } = false;

	public ESP32DeviceInfo deviceInfo { get; private set; }
	public ESP32InputState currentState { get; private set; }

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

	float heartbeatInterval = 5;
	public string name { get; private set; }

	public ESP32Sender sender { get; private set; }
	public ESP32Receiver receiver { get; private set; }
	public bool IsDisposed { get; private set; }

	public ConnectionState connectionState { get; private set; }

	public ESP32Device(ESP32ClientSettings settings, ESP32Receiver espReceiver)
	{
		name = settings.name;
		autoConnectInBuild = settings.autoConnectInBuild;
		sender = new ESP32Sender(settings.address, settings.port);

		receiver = espReceiver;
		receiver.OnInfo += OnInfo;
		receiver.OnInput += OnInput;
		receiver.OnDisconnect += OnDisconnect;
		receiver.OnAlive += OnAlive;
	}


	public void Dispose()
	{
		if (sender != null)
		{
			sender.Dispose();
			sender = null;
		}

		if (receiver != null)
		{
			receiver.OnInfo -= OnInfo;
			receiver.OnInput -= OnInput;
			receiver.OnDisconnect -= OnDisconnect;
			receiver.OnAlive -= OnAlive;
			receiver = null;
		}

		IsDisposed = true;
	}

	public void Update()
	{
		connectionStateTime += Time.deltaTime;

		if (timeSinceLastEvent >= 0)
			timeSinceLastEvent += Time.deltaTime;


		switch (connectionState)
		{
			case ConnectionState.Connected:
			{
				if (Time.time - heartbeatSendTime > heartbeatInterval)
				{
					if (heartbeatState == HeartbeatState.WaitingForResponse) // previous heartbeat didn't get a response
					{
						failedHeartbeats++;
						Debug.LogWarning($"Heartbeat {heartbeatMsgId} didn't get a response (failures: {failedHeartbeats})");
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

			case ConnectionState.Disconnected:
				if (!Application.isEditor && autoConnectInBuild)
				{
					Connect();
				}

				break;
		}
	}

	public void OnInput(ESP32Event<ESP32InputState> evt)
	{
		if (evt.senderAddress != sender.address)
			return;

		if (connectionState == ConnectionState.Connected)
		{
			var state = evt.data;

			currentState = state;

			timeSinceLastEvent = 0;

			OnInputReceived?.Invoke(this, evt.data);
		}
	}

	void OnDisconnect(ESP32Event<ESP32DisconnectInfo> evt)
	{
		if (evt.senderAddress != sender.address)
			return;
		if (connectionState == ConnectionState.Connected)
		{
			SetState(ConnectionState.Disconnected);
		}
	}

	void OnInfo(ESP32Event<ESP32DeviceInfo> evt)
	{
		if (evt.senderAddress != sender.address)
			return;

		if (connectionState == ConnectionState.Connecting)
		{
			SetState(ConnectionState.Connected);
		}
		if (connectionState == ConnectionState.Connected )
		{
			deviceInfo = evt.data;
			timeSinceLastEvent = 0;
		}
	}

	void OnAlive(ESP32Event<ESP32AliveMessage> evt)
	{
		if (evt.senderAddress != sender.address)
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
			sender.SendMotorSpeed(speed);
	}

	public void SendHapticEvent(int hapticEventId)
	{
		if (connectionState == ConnectionState.Connected)
			sender.SendHapticEvent(hapticEventId);
	}

	public void Connect()
	{
		if (connectionState == ConnectionState.Disconnected)
		{
			SetState(ConnectionState.Connecting);
			sender.Connect(receiver.address, receiver.port);
		}
	}


	public void Disconnect()
	{
		if (connectionState == ConnectionState.Connected)
		{
			sender.Disconnect();
			SetState(ConnectionState.Disconnected);
		}
	}

	public void SendHeartbeat()
	{
		heartbeatMsgId++;
		sender.SendHeartbeat(heartbeatMsgId);
		heartbeatSendTime = Time.time;
		heartbeatState = HeartbeatState.WaitingForResponse;
		//Debug.Log($"send heartbeat {heartbeatMsgId}");
	}

	void SetState(ConnectionState newState)
	{
		if (connectionState == newState)
		{
			Debug.LogWarning($"Can't set state: {newState}, already set");
			return;
		}

		// LEAVE STATE
		switch (connectionState)
		{
			case ConnectionState.Disconnected:
				break;
			case ConnectionState.Connecting:
				break;
			case ConnectionState.Connected:
				OnDisconnected?.Invoke(this);
				break;
			default:
				throw new ArgumentOutOfRangeException();
		}

		var prevState = connectionState;
		connectionState = newState;
		connectionStateTime = 0;

		// ENTER STATE
		switch (connectionState)
		{
			case ConnectionState.Disconnected:
				heartbeatState = HeartbeatState.Idle;
				failedHeartbeats = 0;
				lastHeartbeatRTT = 0;
				currentState = new ESP32InputState
				{
					button = false,
					encoder = 0
				};
				break;
			case ConnectionState.Connected:
				OnConnected?.Invoke(this);
				break;
		}
	}

	public void Reboot()
	{
		sender.SendReboot();
	}

	public void Sleep()
	{
		sender.SendSleep();
	}
}