#if UNITY_INPUT_SYSTEM
using UnityEngine;
using UnityEngine.InputSystem;

public class ESP32Example_MotorButton : MonoBehaviour
{
	public InputAction button;

	Esp32InputDevice lastInputDevice;

	void Start()
	{
		button.Enable();
	}

	void Update()
	{
		if (button.activeControl != null)
		{
			lastInputDevice = button.activeControl.device as Esp32InputDevice;
		}

		if (lastInputDevice != null)
		{
			if (button.WasPressedThisFrame())
				lastInputDevice.SendHapticEvent(64);

			if (button.WasReleasedThisFrame())
				lastInputDevice.SendHapticEvent(47);
		}
	}
}

#endif