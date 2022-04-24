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
        
        EditorGUILayout.LabelField("Local IP",espTarget.serverAddress);
        EditorGUILayout.LabelField("Server Port",espTarget.serverPort.ToString());


        var state = "";
        var isActive = espTarget.timeSinceLastEvent >= 0;
        var color = Color.white;
        if (isActive)
        {
            color = new Color(.0f,1,.0f);
            state = "connected";
        }
        else
        {
            color = new Color(1f, .0f, .0f);
            state = "not connected";
        }
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("","Connection",EditorStyles.boldLabel);

        GUILayout.BeginHorizontal();
        EditorGUILayout.PrefixLabel("State");
        var prevColor = GUI.color;
        GUI.color = color;
        EditorGUILayout.LabelField(state,EditorStyles.boldLabel);
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
        
        EditorGUILayout.Separator();
        EditorGUILayout.LabelField("","Debug ",EditorStyles.largeLabel);
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