using System;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Esp32OscConnectionManager))]
[CanEditMultipleObjects]
public class Esp32OscConnectionManagerEditor : Editor
{
	public override bool RequiresConstantRepaint()
	{
		return true;
	}

	public override void OnInspectorGUI()
	{
		base.OnInspectorGUI();
		var connectionManager = (target as Esp32OscConnectionManager);

		EditorGUILayout.LabelField("Server (Local) Address", connectionManager.server != null ? connectionManager.server.address : "none");
		EditorGUILayout.LabelField("Server (Local) Port", connectionManager.server != null ? connectionManager.server.port.ToString() : "none");

		UnityEditor.EditorApplication.QueuePlayerLoopUpdate();
	}
}