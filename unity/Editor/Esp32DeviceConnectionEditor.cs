using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(Esp32DeviceConnection))]
public class Esp32DeviceConnectionEditor : Editor
{
    private int hapticEvent;
    private float motorSpeed;
    
    public override bool RequiresConstantRepaint()
    {
        return true;
    }
    
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        var espTarget = (target as Esp32DeviceConnection);
        
        EditorGUILayout.LabelField("Server (Local) Address",espTarget.serverAddress);
        EditorGUILayout.LabelField("Server (Local) Port",espTarget.serverPort.ToString());

        var isConnected = espTarget.timeSinceLastEvent >= 0;
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("","Connection",EditorStyles.boldLabel);

        GUILayout.BeginHorizontal();
        EditorGUILayout.PrefixLabel("State");
        var prevColor = GUI.color;
        GUI.color = isConnected ?  new Color(.0f,1,.0f) :   new Color(1f, .0f, .0f);
        EditorGUILayout.LabelField(isConnected ? "connected" : "disconnected",EditorStyles.boldLabel);
        GUILayout.EndHorizontal();
        GUI.color = prevColor;
        
        GUILayout.BeginHorizontal();
        EditorGUILayout.PrefixLabel("Time since last event");
        EditorGUILayout.LabelField(espTarget.timeSinceLastEvent >= 0 ? $"{espTarget.timeSinceLastEvent:0.0}s" : "no events received");
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        EditorGUILayout.PrefixLabel(" ");
        if (GUILayout.Button("Connect"))
        {
            espTarget.gameObject.SetActive(false);
            espTarget.gameObject.SetActive(true);
            espTarget.SendIpNow();
        }

        GUILayout.EndHorizontal();
        
        if(isConnected)
        {
            EditorGUILayout.Space();
            GUI.enabled = false;
            EditorGUILayout.LabelField("Device Info", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.LabelField("Device Name", espTarget.deviceInfo.name);
            EditorGUILayout.LabelField("Firmware Version", $"{espTarget.deviceInfo.firmwareVersion}");
            EditorGUILayout.LabelField("Battery Voltage", $"{espTarget.deviceInfo.batteryVoltage:0.0}V");
            
            GUILayout.BeginHorizontal();
            {
                EditorGUILayout.PrefixLabel("Battery Level");
                var prevColor2 = GUI.color;
                GUI.color = espTarget.deviceInfo.batteryLevel > 0.2f ? (espTarget.deviceInfo.batteryLevel > 0.5f ? new Color(.0f, 1, .0f) : new Color(1f, 1f, .0f)) : new Color(1f, .0f, .0f);
                EditorGUI.indentLevel--;
                EditorGUILayout.LabelField($"{Mathf.RoundToInt(espTarget.deviceInfo.batteryLevel * 100)}%");
                EditorGUI.indentLevel++;
                GUI.color = prevColor2;
            }
            GUILayout.EndHorizontal();
            
            EditorGUILayout.LabelField("Has Motor", $"{espTarget.deviceInfo.hasMotor}");
            GUI.enabled = true;
            EditorGUI.indentLevel--;
        }

        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("","Input",EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Button:",espTarget.currentState.button ? "down" : "up",EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Encoder Value:",espTarget.currentState.encoder.ToString(),EditorStyles.boldLabel);
        EditorGUILayout.Space();

        EditorGUILayout.LabelField("","Output",EditorStyles.boldLabel);
        hapticEvent = EditorGUILayout.IntSlider("Haptic Event", hapticEvent, 0, 123);
           
        GUILayout.BeginHorizontal();
        EditorGUILayout.PrefixLabel(" ");
        if (GUILayout.Button("Send Haptic Event"))
        {
            espTarget.SendHapticEvent(hapticEvent);
        }
 
        GUILayout.EndHorizontal();

       var newMotorSpeed = EditorGUILayout.Slider("Motor Speed", motorSpeed, 0f, 1f);
       
       if(Math.Abs(motorSpeed - newMotorSpeed) > 0.00001f)
       {
           espTarget.SendMotorSpeed(motorSpeed);
           motorSpeed = newMotorSpeed;
       }
           
        GUILayout.BeginHorizontal();
        EditorGUILayout.PrefixLabel(" ");
        if (GUILayout.Button("Stop Motor"))
        {
            espTarget.SendMotorSpeed(0);
            motorSpeed = 0;
        }

        GUILayout.EndHorizontal();
        
        #if UNITY_INPUT_SYSTEM
        EditorGUILayout.Separator();
        EditorGUILayout.LabelField("Input System ",EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Device Path", espTarget.inputDevice != null ? espTarget.inputDevice.name : "(no device)",EditorStyles.boldLabel);
        #endif
        UnityEditor.EditorApplication.QueuePlayerLoopUpdate();
    }
}