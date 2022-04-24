#if UNITY_INPUT_SYSTEM

using System.Runtime.InteropServices;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Utilities;

[StructLayout(LayoutKind.Explicit, Size = kSize)]
internal struct Esp32HapticRealtimeCommand : IInputDeviceCommandInfo
{
    public static FourCC Type { get { return new FourCC('H', 'P', 'T', 'R'); } }

    internal const int kSize = InputDeviceCommand.BaseCommandSize + sizeof(int);

    [FieldOffset(0)]
    public InputDeviceCommand baseCommand;

    [FieldOffset(InputDeviceCommand.BaseCommandSize)]
    public float speed;

    public FourCC typeStatic
    {
        get { return Type; }
    }

    public static Esp32HapticRealtimeCommand Create(float speed)
    {
        return new Esp32HapticRealtimeCommand
        {
            baseCommand = new InputDeviceCommand(Type, kSize),
            speed = speed
        };
    }
}

#endif