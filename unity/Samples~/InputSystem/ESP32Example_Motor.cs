using UnityEngine;
using UnityEngine.InputSystem;

public class ESP32Example_Motor : MonoBehaviour
{
	public int eventId = 47;
	
	void Update()
	{
		var device = InputSystem.GetDevice<Esp32InputDevice>();

		if (device != null)
		{
			if (Keyboard.current.spaceKey.wasPressedThisFrame)
				device.SendHapticEvent(eventId);
		}
	}
}