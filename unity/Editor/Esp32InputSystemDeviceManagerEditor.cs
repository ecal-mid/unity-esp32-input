
#if UNITY_INPUT_SYSTEM
using System;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Esp32InputSystemDeviceManager))]
[CanEditMultipleObjects]
public class Esp32InputSystemDeviceManagerEditor : Editor
{
	bool foldout;
	public override bool RequiresConstantRepaint()
	{
		return true;
	}

	public override void OnInspectorGUI()
	{
		base.OnInspectorGUI();
		var _target = (target as Esp32InputSystemDeviceManager);

		EditorGUI.indentLevel++;
		foldout = EditorGUILayout.Foldout(foldout,"Input Devices");
		if (foldout)
		{
			foreach (var pair in _target.inputDevices)
			{
				var esp32Device = pair.Key;
				var esp32InputDevice = pair.Value;
				EditorGUILayout.LabelField(esp32Device.name, esp32InputDevice.name);
			}
		}
		EditorGUI.indentLevel--;
	}
}

#endif