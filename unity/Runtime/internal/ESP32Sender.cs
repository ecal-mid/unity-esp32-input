using System;
using OscJack;
using UnityEngine;

[Serializable]
public class ESP32Sender : IDisposable
{
	
	public string address { get; private set; }
	public int port { get; private set; }
	public OscClient oscClient { get; private set; }

	public ESP32Sender(string address, int port)
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

	public void SendMotorSpeed(int motorId, float speed)
	{
		oscClient.Send("/arduino/motor/rt", motorId, Mathf.RoundToInt(speed * 100));
	}
	public void StopMotors()
	{
		oscClient.Send("/arduino/motor/stopall");
	}

	public void SendHapticEvent(int motorId,int hapticEventId)
	{
		oscClient.Send("/arduino/motor/cmd", motorId, hapticEventId);
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