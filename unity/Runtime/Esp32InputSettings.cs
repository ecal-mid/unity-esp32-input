using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class Esp32InputSettings : ScriptableObject
{
	public int serverPort = 8888;
	public List<Esp32ClientConnectionSettings> clients;

}