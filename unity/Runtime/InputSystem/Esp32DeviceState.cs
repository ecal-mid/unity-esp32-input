#if UNITY_INPUT_SYSTEM

using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Utilities;

public struct Esp32DeviceState : IInputStateTypeInfo
{
    // We use "CUST" here as our custom format code. It can be anything really.
    // Should be sufficiently unique to identify our memory format, though.
    public FourCC format => new FourCC('E', 'S', 'P', '3');

    // Next we just define fields that store the state for our input device.
    // The only thing really interesting here is the [InputControl] attributes.
    // These automatically attach InputControls to the various memory bits that
    // we define.
    //
    // To get started, let's say that our device has a bitfield of buttons. Each
    // bit indicates whether a certain button is pressed or not. For the sake of
    // demonstration, let's say our device has 16 possible buttons. So, we define
    // a ushort field that contains the state of each possible button on the
    // device.
    //
    // On top of that, we need to tell the input system about each button. Both
    // what to call it and where to find it. The "name" property tells the input system
    // what to call the control; the "layout" property tells it what type of control
    // to create ("Button" in our case); and the "bit" property tells it which bit
    // in the bitfield corresponds to the button.
    //
    // We also tell the input system about "display names" here. These are names
    // that get displayed in the UI and such.
    //[InputControl(name = "firstButton", layout = "Button", bit = 0, displayName = "First Button")]
    //[InputControl(name = "secondButton", layout = "Button", bit = 1, displayName = "Second Button")]
    //[InputControl(name = "thirdButton", layout = "Button", bit = 2, displayName = "Third Button")]
    // public ushort buttons;

    // Let's say our device also has a stick. However, the stick isn't stored
    // simply as two floats but as two unsigned bytes with the midpoint of each
    // axis located at value 127. We can simply define two consecutive byte
    // fields to represent the stick and annotate them like so.
    //
    // First, let's introduce stick control itself. This one is simple. We don't
    // yet worry about X and Y individually as the stick as whole will itself read the
    // component values from those controls.
    //
    // We need to set "format" here too as InputControlLayout will otherwise try to
    // infer the memory format from the field. As we put this attribute on "X", that
    // would come out as "BYTE" -- which we don't want. So we set it to "VC2B" (a Vector2
    // of bytes).
    //[InputControl(name = "stick", format = "VC2B", layout = "Stick", displayName = "Main Stick")]
    // So that's what we need next. By default, both X and Y on "Stick" are floating-point
    // controls so here we need to individually configure them the way they work for our
    // stick.
    //
    // NOTE: We don't mention things as "layout" and such here. The reason is that we are
    //       modifying a control already defined by "Stick". This means that we only need
    //       to set the values that are different from what "Stick" stick itself already
    //       configures. And since "Stick" configures both "X" and "Y" to be "Axis" controls,
    //       we don't need to worry about that here.
    //
    // Using "format", we tell the controls how their data is stored. As bytes in our case
    // so we use "BYTE" (check the documentation for InputStateBlock for details on that).
    //
    // NOTE: We don't use "SBYT" (signed byte) here. Our values are not signed. They are
    //       unsigned. It's just that our "resting" (i.e. mid) point is at 127 and not at 0.
    //
    // Also, we use "defaultState" to tell the system that in our case, setting the
    // memory to all zeroes will *NOT* result in a default value. Instead, if both x and y
    // are set to zero, the result will be Vector2(-1,-1).
    //
    // And then, using the various "normalize" parameters, we tell the input system how to
    // deal with the fact that our midpoint is located smack in the middle of our value range.
    // Using "normalize" (which is equivalent to "normalize=true") we instruct the control
    // to normalize values. Using "normalizeZero=0.5", we tell it that our midpoint is located
    // at 0.5 (AxisControl will convert the BYTE value to a [0..1] floating-point value with
    // 0=0 and 255=1) and that our lower limit is "normalizeMin=0" and our upper limit is
    // "normalizeMax=1". Put another way, it will map [0..1] to [-1..1].
    //
    // Finally, we also set "offset" here as this is already set by StickControl.X and
    // StickControl.Y -- which we inherit. Note that because we're looking at child controls
    // of the stick, the offset is relative to the stick, not relative to the beginning
    // of the state struct.
    //[InputControl(name = "stick/x", defaultState = 127, format = "BYTE",
    //    offset = 0,
    //    parameters = "normalize,normalizeMin=0,normalizeMax=1,normalizeZero=0.5")]
    //public byte x;
    //[InputControl(name = "stick/y", defaultState = 127, format = "BYTE",
    //   offset = 1,
    //   parameters = "normalize,normalizeMin=0,normalizeMax=1,normalizeZero=0.5")]
    // The stick up/down/left/right buttons automatically use the state set up for X
    // and Y but they have their own parameters. Thus we need to also sync them to
    // the parameter settings we need for our BYTE setup.
    // NOTE: This is a shortcoming in the current layout system that cannot yet correctly
    //       merge parameters. Will be fixed in a future version.
    //[InputControl(name = "stick/up", parameters = "normalize,normalizeMin=0,normalizeMax=1,normalizeZero=0.5,clamp=2,clampMin=0,clampMax=1")]
    // [InputControl(name = "stick/down", parameters = "normalize,normalizeMin=0,normalizeMax=1,normalizeZero=0.5,clamp=2,clampMin=-1,clampMax=0,invert")]
    //[InputControl(name = "stick/left", parameters = "normalize,normalizeMin=0,normalizeMax=1,normalizeZero=0.5,clamp=2,clampMin=-1,clampMax=0,invert")]
    //[InputControl(name = "stick/right", parameters = "normalize,normalizeMin=0,normalizeMax=1,normalizeZero=0.5,clamp=2,clampMin=0,clampMax=1")]
    //public byte y;


    [InputControl(layout = "Axis")] public float encoder;

    [InputControl(layout = "Button")] public bool button;

    public override string ToString()
    {
        return $"{nameof(encoder)}: {encoder}, {nameof(button)}: {button}";
    }
    
	public static implicit operator Esp32DeviceState(Esp32InputState state)
    {
        return new Esp32DeviceState
        {
            button = state.button,
            encoder = state.encoder
        };
    }
}

#endif