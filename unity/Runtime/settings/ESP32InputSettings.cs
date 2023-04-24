using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ESP32InputSettings  
{
	public int serverPort = 8888;
	public List<ESP32ClientSettings> clients;
}