#if UNITY_INPUT_SYSTEM

using System.Runtime.InteropServices;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Utilities;

[StructLayout(LayoutKind.Explicit, Size = kSize)]
internal struct Esp32HapticRealtimeCommand : IInputDeviceCommandInfo
{
    public static FourCC Type => new('H', 'P', 'T', 'R');

    internal const int kSize = InputDeviceCommand.BaseCommandSize + sizeof(int);

    [FieldOffset(0)]
    public InputDeviceCommand baseCommand;

    [FieldOffset(InputDeviceCommand.BaseCommandSize)]
    public int motorId;

    [FieldOffset(sizeof(int))]
    public float speed;

    public FourCC typeStatic => Type;

    public static Esp32HapticRealtimeCommand Create(int motorId, float speed)
    {
        return new Esp32HapticRealtimeCommand
        {
            baseCommand = new InputDeviceCommand(Type, kSize),
            speed = speed,
            motorId = motorId
        };
    }
}

#endif