#if UNITY_INPUT_SYSTEM

using System.Runtime.InteropServices;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Utilities;

[StructLayout(LayoutKind.Explicit, Size = kSize)]
internal struct Esp32HapticEventCommand : IInputDeviceCommandInfo
{
    public static FourCC Type => new('H', 'P', 'T', 'E');

    internal const int kSize = InputDeviceCommand.BaseCommandSize + sizeof(int);

    [FieldOffset(0)]
    public InputDeviceCommand baseCommand;

    [FieldOffset(InputDeviceCommand.BaseCommandSize)]
    public int motorId;

    [FieldOffset(sizeof(int))]
    public int eventId;

    public FourCC typeStatic => Type;

    public static Esp32HapticEventCommand Create(int motorId, int eventId)
    {
        return new Esp32HapticEventCommand
        {
            baseCommand = new InputDeviceCommand(Type, kSize),
            eventId = eventId,
            motorId = motorId
        };
    }
}

#endif