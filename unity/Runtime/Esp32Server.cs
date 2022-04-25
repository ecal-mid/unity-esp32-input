using System;
using System.Net;
using System.Net.Sockets;
using OscJack;
using UnityEngine;

[Serializable]
public class Esp32Server: IDisposable
{
	public event Action<Esp32Event<Esp32InputState>> OnInput;
	public event Action<Esp32Event<ESP32DeviceInfo>> OnInfo;
	public event Action<Esp32Event<ESP32DisconnectInfo>> OnDisconnect;

	public int port = 8888;
	public string address { get; private set; }

	OscServer server; // IN


	public Esp32Server(int serverPort)
	{
		port = serverPort;
		server = new OscServer(port);
		server.MessageDispatcher.AddCallback("/unity/state/", OnDataReceive);
		server.MessageDispatcher.AddCallback("/unity/info/", OnDataReceive);
		server.MessageDispatcher.AddCallback("/unity/disconnect/", OnDataReceive);

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


	void OnDataReceive(string oscAddress, OscDataHandle data)
	{
		Debug.Log(oscAddress);
		var deviceAddress = data.GetElementAsString(0);
		switch (oscAddress)
		{
			case "/unity/disconnect/":
			{
				if (OnDisconnect != null)
					OnDisconnect(new Esp32Event<ESP32DisconnectInfo>()
					{
						senderAddress = deviceAddress,
						data = default
					});
				break;
			}
			case "/unity/state/":
			{
				var state = new Esp32InputState();
				state.button = data.GetElementAsInt(1) == 0; // 0 = pressed, 1 = released
				state.encoder = data.GetElementAsInt(2) / 4095f;

				if (OnInput != null)
					OnInput(new Esp32Event<Esp32InputState>()
					{
						senderAddress = deviceAddress,
						data = state
					});
				break;
			}
			case "/unity/info/":
			{
				var minVoltage = 3.6f;
				var maxVoltage = 4.2f;

				var deviceInfo = new ESP32DeviceInfo
				{
					name = data.GetElementAsString(1),
					firmwareVersion = data.GetElementAsInt(2),
					batteryVoltage = data.GetElementAsFloat(3),
					batteryLevel = Mathf.InverseLerp(minVoltage, maxVoltage, data.GetElementAsFloat(3)),
					hasMotor = data.GetElementAsInt(4) == 1,
				};

				if (OnInfo != null)
					OnInfo(new Esp32Event<ESP32DeviceInfo>
					{
						senderAddress = deviceAddress,
						data = deviceInfo
					});
				break;
			}
		}
	}

}