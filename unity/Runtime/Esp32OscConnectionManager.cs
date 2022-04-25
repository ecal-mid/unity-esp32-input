using System;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-12)]
[ExecuteAlways]
public class Esp32OscConnectionManager : MonoBehaviour
{
	public int serverPort = 8888;
	
	public Esp32Server server { get; private set; } 
	List<Esp32Client> clients = new List<Esp32Client>(); // OUT

	void OnEnable()
	{
		server = new Esp32Server(serverPort);
	}

	void OnDisable()
	{
		for (int i = clients.Count - 1; i >= 0; i--)
		{
			RemoveClient(clients[i]);
		}

		server.Dispose();
		server = null;
	}

	public Esp32Client AddClient(string address, int port)
	{
		var client = new Esp32Client(address,port);
		clients.Add(client);

		return client;
	}

	public void RemoveClient(Esp32Client client)
	{
		if (client != null)
		{
			client.Dispose();
			clients.Remove(client);
		}

	}
}