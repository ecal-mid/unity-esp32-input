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

	public event Action<ESP32Device,ESP32InputState> OnInputReceived;
	public float connectionStateTime { get; private set; } = 0;
	public float timeSinceLastEvent { get; private set; } = -1;

	public bool autoConnectEnabled => !Application.isEditor;

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
	bool firstEncoderValueReceived = false;
	float zeroEncoderValue;
	public string name;

	public ESP32Sender sender { get; private set; }
	public ESP32Receiver receiver { get; private set; }
	public bool IsDisposed { get; private set; }

	public ConnectionState connectionState { get; private set; }

	public ESP32Device(ESP32ClientSettings settings, ESP32Receiver espReceiver)
	{
		name = settings.name;
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
				if (autoConnectEnabled)
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

			if (!firstEncoderValueReceived)
			{
				zeroEncoderValue = state.encoder;
				firstEncoderValueReceived = true;
			}

			state.encoder -= zeroEncoderValue;

			currentState = state;

			timeSinceLastEvent = 0;

			OnInputReceived?.Invoke(this,evt.data);
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
		if (connectionState == ConnectionState.Connected || connectionState == ConnectionState.Connecting)
		{
			deviceInfo = evt.data;
			timeSinceLastEvent = 0;

			SetState(ConnectionState.Connected);
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

	void SetState(ConnectionState connectionState)
	{
		this.connectionState = connectionState;
		connectionStateTime = 0;

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