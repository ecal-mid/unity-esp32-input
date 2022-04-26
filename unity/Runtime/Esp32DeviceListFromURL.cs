using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[DefaultExecutionOrder(-20)]
public class Esp32DeviceListFromURL : MonoBehaviour
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
		GetComponent<Esp32DeviceManager>().enabled = false;
		Load(() =>
		{
			GetComponent<Esp32DeviceManager>().enabled = true;
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
			var deviceManager = GetComponent<Esp32DeviceManager>();
			deviceManager.settings.clients.Clear();
			for (int i = 0; i < list.data.Count; i++)
			{
				deviceManager.settings.clients.Add(new Esp32ClientConnectionSettings
				{
					name = list.data[i].name,
					address = list.data[i].ip,
					port = 9999
				});
			}

			deviceManager.Restart();

			if (onComplete != null)
				onComplete();
		});
	}
}