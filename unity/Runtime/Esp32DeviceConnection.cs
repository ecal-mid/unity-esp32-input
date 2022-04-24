using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using UnityEngine;
using OscJack;
using Unity.Collections;
#if UNITY_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.LowLevel;
#endif

[ExecuteAlways]
public class Esp32DeviceConnection : MonoBehaviour
{
	public struct ESP32DeviceInfo
	{
		public string name;
		public int firmwareVersion;
		public float batteryVoltage;
		public float batteryLevel;
		public bool hasMotor;

		public override string ToString()
		{
			return $"{nameof(name)}: {name}, {nameof(firmwareVersion)}: {firmwareVersion}, {nameof(batteryLevel)}: {batteryLevel}, {nameof(hasMotor)}: {hasMotor}";
		}
	}

	public struct InputState
	{
		public float encoder;
		public bool button;
	}

	[Header("Client")]
	public string clientAddress = "192.168.45.30";

	public int clientPort = 9999;

	[Header("Server")]
	public int serverMinPort = 8880;

	public int serverMaxPort = 8899;
	public string serverAddress { get; private set; }
	public int serverPort { get; private set; }

	OscServer _server; // IN
	OscClient _client; // OUT

	Queue<InputState> dataQueue = new Queue<InputState>();

	public float timeSinceLastEvent { get; private set; }
	public float timeSinceLastIpSend { get; private set; }
	public float timeSinceLastHeartbeat { get; private set; }

	public bool ipAutoSendEnabled => !Application.isEditor;
	float ipSendInterval = 5;
	float heartbeatInterval = 5;

	public bool initialized { get; private set; }

	public ESP32DeviceInfo deviceInfo;
	public InputState currentState { get; private set; }

#if UNITY_INPUT_SYSTEM
	public Esp32Device inputDevice { get; private set; }
#endif

	bool firstEncoderValueReceived;
	float zeroEncoderValue;

	void ResetState()
	{
		timeSinceLastEvent = -1;
		timeSinceLastIpSend = 999;
		timeSinceLastHeartbeat = 999;
		serverPort = -1;
		initialized = false;
		firstEncoderValueReceived = false;
		dataQueue.Clear();
		serverPort = 0;
		serverAddress = null;


#if UNITY_INPUT_SYSTEM
		if (inputDevice != null)
		{
			InputSystem.RemoveDevice(inputDevice);
			inputDevice = null;
		}

		RemoveCommandListener();
#endif

		if (_server != null)
		{
			_server.Dispose();
			_server = null;
		}

		if (_client != null)
		{
			_client.Dispose();
			_client = null;
		}

	}

	void Awake()
	{
		ResetState();
	}

	void OnEnable()
	{
		// IN
		serverPort = serverMinPort;
		while (serverPort < serverMaxPort)
		{
			try
			{
				_server = new OscServer(serverPort); // Port number
				break;
			}
			catch
			{
			}

			serverPort++;
		}

		if (_server == null)
			throw new UnityException($"No free server port found in range {serverMinPort}-{serverMaxPort}");

		_server.MessageDispatcher.AddCallback("/unity/state/", OnDataReceiveState);
		_server.MessageDispatcher.AddCallback("/unity/info/", OnDataReceiveInfo);

		// OUT
		_client = new OscClient(clientAddress, clientPort);

		// start update ip loop
		serverAddress = GetLocalAddress();

#if UNITY_INPUT_SYSTEM
		inputDevice = InputSystem.AddDevice(new InputDeviceDescription
		{
			interfaceName = "ESP32",
			product = "Input Thing",
			version = "1",
			deviceClass = "box",
			manufacturer = "ecal",
			capabilities = "encoder,button,motor",
			serial = $"{clientAddress}:{clientPort}@{serverPort}",
		}) as Esp32Device;

		InputSystem.EnableDevice(inputDevice);

		AddCommandListener();
#endif

		initialized = true;
	}


	void OnDisable()
	{
		ResetState();
	}


	void Update()
	{
		lock (dataQueue)
		{
			while (dataQueue.Count > 0)
			{
				var state = dataQueue.Dequeue();

#if UNITY_INPUT_SYSTEM
				InputSystem.QueueStateEvent<Esp32DeviceState>(inputDevice, state);
#endif

				currentState = state;
			}
		}

		if (timeSinceLastEvent >= 0)
			timeSinceLastEvent += Time.deltaTime;

		if (ipAutoSendEnabled)
		{
			timeSinceLastIpSend += Time.deltaTime;
			if (timeSinceLastIpSend > ipSendInterval)
			{
				SendIpNow();
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

	void OnDataReceiveState(string address, OscDataHandle data)
	{
		var state = new InputState();
		state.button = data.GetElementAsInt(0) == 0; // 0 = pressed, 1 = released
		state.encoder = data.GetElementAsInt(1) / 4095f;

		if (!firstEncoderValueReceived)
		{
			zeroEncoderValue = state.encoder;
			firstEncoderValueReceived = true;
		}

		state.encoder -= zeroEncoderValue;

		lock (dataQueue)
		{
			dataQueue.Enqueue(state);
		}

		timeSinceLastEvent = 0;
	}


	void OnDataReceiveInfo(string address, OscDataHandle data)
	{
		var minVoltage = 3.6f;
		var maxVoltage = 4.2f;
		deviceInfo = new ESP32DeviceInfo
		{
			name = data.GetElementAsString(0),
			firmwareVersion = data.GetElementAsInt(1),
			batteryVoltage =  data.GetElementAsFloat(2),
			batteryLevel = Mathf.InverseLerp(minVoltage,maxVoltage, data.GetElementAsFloat(2) ),
			hasMotor = data.GetElementAsInt(3) == 1,
		};

		timeSinceLastEvent = 0;
	}

	public void SendHeartbeat()
	{
		_client.Send("/arduino/keepalive", 0);
	}

	public void SendIpNow()
	{
		_client.Send("/arduino/updateip", $"{serverAddress}:{serverPort}");
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
#if UNITY_INPUT_SYSTEM
	unsafe void AddCommandListener()
	{
		InputSystem.onDeviceCommand += OnDeviceCommand;
	}

	unsafe void RemoveCommandListener()
	{
		InputSystem.onDeviceCommand -= OnDeviceCommand;
	}

	private unsafe long? OnDeviceCommand(InputDevice commandDevice, InputDeviceCommand* command)
	{
		if (commandDevice == inputDevice)
		{
			if (command->type == Esp32HapticEventCommand.Type)
			{
				var cmd = (Esp32HapticEventCommand*)command;
				SetHapticEventToClient(cmd->eventId);
				return 0;
			}

			if (command->type == Esp32HapticRealtimeCommand.Type)
			{
				var cmd = (Esp32HapticRealtimeCommand*)command;
				SendMotorSpeedToClient(cmd->speed);
				return 0;
			}
		}

		return null;
	}
#endif

	public void SendMotorSpeed(float speed)
	{
#if UNITY_INPUT_SYSTEM
		inputDevice.SendMotorSpeed(speed);
#else
		SendMotorSpeedToClient(speed);
#endif
	}

	public void SendHapticEvent(int hapticEventId)
	{
#if UNITY_INPUT_SYSTEM
		inputDevice.SendHapticEvent(hapticEventId);
#else
		SetHapticEventToClient(hapticEventId);
#endif
	}

	void SendMotorSpeedToClient(float speed)
	{
		_client.Send("/arduino/motor/rt", Mathf.RoundToInt(speed * 100));
	}

	void SetHapticEventToClient(int hapticEventId)
	{
		_client.Send("/arduino/motor/cmd", hapticEventId);
	}
}