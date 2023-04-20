using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using OscJack;
using UnityEngine;

[Serializable]
public class ESP32Receiver : IDisposable
{
	ConcurrentQueue<ESP32Event<ESP32DeviceInfo>> infoEvents = new ConcurrentQueue<ESP32Event<ESP32DeviceInfo>>();
	ConcurrentQueue<ESP32Event<ESP32ButtonInputState>> inputButtonEvents = new ConcurrentQueue<ESP32Event<ESP32ButtonInputState>>();
	ConcurrentQueue<ESP32Event<ESP32EncoderInputState>> inputEncoderEvents = new ConcurrentQueue<ESP32Event<ESP32EncoderInputState>>();
	ConcurrentQueue<ESP32Event<ESP32DisconnectInfo>> disconnectEvents = new ConcurrentQueue<ESP32Event<ESP32DisconnectInfo>>();
	ConcurrentQueue<ESP32Event<ESP32AliveMessage>> aliveEvents = new ConcurrentQueue<ESP32Event<ESP32AliveMessage>>();

	public event Action<ESP32Event<ESP32ButtonInputState>> OnButtonInput;
	public event Action<ESP32Event<ESP32EncoderInputState>> OnEncoderInput;
	public event Action<ESP32Event<ESP32DeviceInfo>> OnInfo;
	public event Action<ESP32Event<ESP32DisconnectInfo>> OnDisconnect;
	public event Action<ESP32Event<ESP32AliveMessage>> OnAlive;

	public int port = 8888;
	public string address { get; private set; }

	OscServer server; // IN


	public ESP32Receiver(int serverPort)
	{
		port = serverPort;
		server = new OscServer(port);
		server.MessageDispatcher.AddCallback("/unity/state/encoder/", OnDataReceive);
		server.MessageDispatcher.AddCallback("/unity/state/button/", OnDataReceive);
		server.MessageDispatcher.AddCallback("/unity/info/", OnDataReceive);
		server.MessageDispatcher.AddCallback("/unity/disconnect/", OnDataReceive);
		server.MessageDispatcher.AddCallback("/unity/alive/", OnDataReceive);

		// start update ip loop
		address = GetLocalAddress();
	}

	public void Dispose()
	{
		if (server != null)
		{
			server.Dispose();
			server = null;
		}
	}

	static string GetLocalAddress()
	{
		IPHostEntry ipEntry = Dns.GetHostEntry(Dns.GetHostName());
		IPAddress[] addr = ipEntry.AddressList;

		var address = string.Empty;
		for (int i = 0; i < addr.Length; i++)
		{
			if (addr[i].AddressFamily == AddressFamily.InterNetwork)
			{
				//Debug.Log("IP Address {0}: {1} " + " "+  i + " "  +addr[i].ToString());
				address = addr[i].ToString();
				break;
			}
		}

		return address;
	}

	void SendEvents<T>(ConcurrentQueue<T> queue, Action<T> eventAction)
	{
		if (eventAction == null)
			return;
		while (queue.TryDequeue(out T evt))
			eventAction.Invoke(evt);
	}

	public void SendAllEvents()
	{
		SendEvents(infoEvents, OnInfo);
		SendEvents(inputButtonEvents, OnButtonInput);
		SendEvents(inputEncoderEvents, OnEncoderInput);
		SendEvents(aliveEvents, OnAlive);
		SendEvents(disconnectEvents, OnDisconnect);
	}

	void OnDataReceive(string oscAddress, OscDataHandle data)
	{
		var deviceAddress = data.GetElementAsString(0);
		switch (oscAddress)
		{
			case "/unity/alive/":
			{
				aliveEvents.Enqueue(new ESP32Event<ESP32AliveMessage>()
				{
					senderAddress = deviceAddress,
					data = new ESP32AliveMessage
					{
						msgId = data.GetElementAsInt(1)
					}
				});
				break;
			}
			case "/unity/disconnect/":
			{
				disconnectEvents.Enqueue(new ESP32Event<ESP32DisconnectInfo>()
				{
					senderAddress = deviceAddress,
					data = default
				});
				break;
			}
			case "/unity/state/button/":
			{
				var state = new ESP32ButtonInputState();
				state.button = data.GetElementAsInt(1) == 1; // 0 = pressed, 1 = released
				inputButtonEvents.Enqueue(new ESP32Event<ESP32ButtonInputState>()
				{
					senderAddress = deviceAddress,
					data = state
				});
				break;
			}
			case "/unity/state/encoder/":
			{
				var state = new ESP32EncoderInputState();
				state.encoder = data.GetElementAsInt(1) / 40f;
				inputEncoderEvents.Enqueue(new ESP32Event<ESP32EncoderInputState>()
				{
					senderAddress = deviceAddress,
					data = state
				});
				break;
			}
			case "/unity/info/":
			{
				var minVoltage = 3.5f;
				var maxVoltage = 4.2f;

				var deviceInfo = new ESP32DeviceInfo
				{
					name = data.GetElementAsString(1),
					firmwareVersion = data.GetElementAsInt(2),
					batteryVoltage = data.GetElementAsFloat(3),
					batteryLevel = Mathf.InverseLerp(minVoltage, maxVoltage, data.GetElementAsFloat(3)),
					motorCount = data.GetElementAsInt(4),
					encoderCount = data.GetElementAsInt(5),
					buttonCount = data.GetElementAsInt(6)
				};

				infoEvents.Enqueue(new ESP32Event<ESP32DeviceInfo>
				{
					senderAddress = deviceAddress,
					data = deviceInfo
				});
				break;
			}
		}
	}
}