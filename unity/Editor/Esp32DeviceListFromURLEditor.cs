using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Esp32DeviceListFromURL))]
[CanEditMultipleObjects]
public class Esp32DeviceListFromURLEditor : Editor
{
	public override void OnInspectorGUI()
	{
		base.OnInspectorGUI();

		var t = target as Esp32DeviceListFromURL;
		
		GUILayout.BeginHorizontal();
		EditorGUILayout.PrefixLabel("Reload from URL");
		if (GUILayout.Button("Reload"))
		{
			t.Load();
		}
		GUILayout.EndHorizontal();
		
	}
}