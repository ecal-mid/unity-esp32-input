using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

static class WebRequestUtils
{
	public static IEnumerator GetJson<T>(string uri, System.Action<T> callback)
	{
		using (UnityWebRequest webRequest = UnityWebRequest.Get(uri))
		{
			yield return webRequest.SendWebRequest();


			switch (webRequest.result)
			{
				case UnityWebRequest.Result.ConnectionError:
				case UnityWebRequest.Result.DataProcessingError:
					Debug.LogError("Error: " + webRequest.error);
					break;
				case UnityWebRequest.Result.ProtocolError:
					Debug.LogError("HTTP Error: " + webRequest.error);
					break;
				case UnityWebRequest.Result.Success:
					// Debug.Log("Received: " + webRequest.downloadHandler.text);

					var text = webRequest.downloadHandler.text;
					var data = JsonUtility.FromJson<T>(text);
					callback(data);
					break;
			}
		}
	}
}