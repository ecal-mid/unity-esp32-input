#if UNITY_INPUT_SYSTEM

using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.LowLevel;
#if UNITY_EDITOR
using UnityEditor;
#endif

#if UNITY_EDITOR
[InitializeOnLoad] // Call static class constructor in editor.
#endif
[InputControlLayout(stateType = typeof(Esp32DeviceState))]
public class Esp32InputDevice : InputDevice//, IInputStateCallbackReceiver
{
    
#if UNITY_EDITOR
    static Esp32InputDevice()
    {
        Initialize();
    }
#endif

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        InputSystem.RegisterLayout<Esp32InputDevice>(
            matches: new InputDeviceMatcher()
                .WithInterface("ESP32"));
    }

    public AxisControl encoder { get; private set; }
    public ButtonControl button { get; private set; }

    protected override void FinishSetup()
    {
        base.FinishSetup();
        
        encoder = GetChildControl<AxisControl>("encoder");
        button = GetChildControl<ButtonControl>("button");

    }
/*
    protected new void OnNextUpdate()
    {
      //  InputState.Change(encoder, 0f);
    }

    protected new unsafe void OnStateEvent(InputEventPtr eventPtr)
    {
       // encoder.AccumulateValueInEvent(currentStatePtr, eventPtr);
      // InputState.Change(this, eventPtr);
    }

    void IInputStateCallbackReceiver.OnNextUpdate()
    {
        OnNextUpdate();
    }

    void IInputStateCallbackReceiver.OnStateEvent(InputEventPtr eventPtr)
    {
        OnStateEvent(eventPtr);
    }

    bool IInputStateCallbackReceiver.GetStateOffsetForEvent(InputControl control, InputEventPtr eventPtr, ref uint offset)
    {
        return false;
    }*/

    public void SendMotorSpeed(float speed) => SendMotorSpeed(0, speed);

    public void SendMotorSpeed(int motorId, float speed)
    {
        var cmd = Esp32HapticRealtimeCommand.Create(motorId,speed);
        ExecuteCommand(ref cmd);
    }


    public void SendHapticEvent(int eventId) => SendHapticEvent(0, eventId);
    public void SendHapticEvent(int motorId, int eventId)
    {
        var cmd = Esp32HapticEventCommand.Create(motorId,eventId);
        ExecuteCommand(ref cmd);
    }
}

#endif