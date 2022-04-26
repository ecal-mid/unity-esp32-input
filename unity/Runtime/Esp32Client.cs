using System;
using OscJack;
using UnityEngine;

[Serializable]
public class Esp32Client : IDisposable
{
	public string address { get; private set; }
	public int port { get; private set; }
	public OscClient oscClient { get; private set; }

	public Esp32Client(string address, int port)
	{
		this.address = address;
		this.port = port;
		oscClient = new OscClient(address, port);
	}

	public void Dispose()
	{
		oscClient?.Dispose();
	}


	public void SendHeartbeat(int msgId)
	{
		oscClient.Send("/arduino/keepalive", msgId);
	}

	public void Connect(string address, int port)
	{
		oscClient.Send("/arduino/connect", $"{address}:{port}");
	}

	public void Disconnect()
	{
		oscClient.Send("/arduino/disconnect");
	}

	public void SendMotorSpeed(float speed)
	{
		oscClient.Send("/arduino/motor/rt", Mathf.RoundToInt(speed * 100));
	}

	public void SendHapticEvent(int hapticEventId)
	{
		oscClient.Send("/arduino/motor/cmd", hapticEventId);
	}

	public void SendReboot()
	{
		oscClient.Send("/arduino/restart");
	}

	public void SendSleep()
	{
		oscClient.Send("/arduino/sleep");
	}
}