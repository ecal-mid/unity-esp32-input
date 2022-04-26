using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ESP32DeviceListFromURL))]
[CanEditMultipleObjects]
public class Esp32DeviceListFromURLEditor : Editor
{
	public override void OnInspectorGUI()
	{
		base.OnInspectorGUI();

		var t = target as ESP32DeviceListFromURL;
		
		GUILayout.BeginHorizontal();
		EditorGUILayout.PrefixLabel("Reload from URL");
		if (GUILayout.Button("Reload"))
		{
			t.Load();
		}
		GUILayout.EndHorizontal();
		
	}
}