using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[DefaultExecutionOrder(-20)]
public class ESP32DeviceListFromURL : MonoBehaviour
{
	[Serializable]
	public struct DeviceList
	{
		public List<DeviceListItem> data;

		[Serializable]
		public struct DeviceListItem
		{
			public string name;
			public string ip;
		}
	}

	public string url = "https://ecal-mid.ch/magicleap/devices.json";

	void Awake()
	{
		GetComponent<ESP32DeviceManager>().enabled = false;
		Load(() =>
		{
			GetComponent<ESP32DeviceManager>().enabled = true;
		});
	}

	public void Load(Action onComplete= null)
	{
		StartCoroutine(UpdateList(onComplete));
	}

	IEnumerator UpdateList(Action onComplete)
	{
		yield return WebRequestUtils.GetJson<DeviceList>(url, list =>
		{
			list.data.Sort((a, b) => a.name.CompareTo(b.name));
			var deviceManager = GetComponent<ESP32DeviceManager>();
			var newClients = new List<ESP32ClientSettings>();
			for (int i = 0; i < list.data.Count; i++)
			{
				var name = list.data[i].name;
				var prevItem = deviceManager.settings.clients.FindIndex(o=> o.name == name);
				newClients.Add(new ESP32ClientSettings
				{
					name = name,
					address = list.data[i].ip,
					port = 9999,
					autoConnectInBuild = prevItem != -1 ? deviceManager.settings.clients[prevItem].autoConnectInBuild : false
				});
			}

			deviceManager.settings.clients = newClients;

			deviceManager.Restart();

			if (onComplete != null)
				onComplete();
		});
	}
}