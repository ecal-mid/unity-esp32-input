using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class ESP32InputSettings : ScriptableObject 
{
	public int serverPort = 8888;
	public List<ESP32ClientSettings> clients;

}