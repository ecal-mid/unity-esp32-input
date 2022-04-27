#if UNITY_INPUT_SYSTEM
using UnityEngine;
using UnityEngine.InputSystem;

public class ESP32Example_MotorSpeed : MonoBehaviour
{
	public InputAction encoder;

	Esp32InputDevice lastInputDevice;
	float lastValue;
	float smoothSpeed = 0;

	void Start()
	{
		encoder.Enable();
	}

	void Update()
	{
		if (encoder.activeControl != null)
		{
			lastInputDevice = encoder.activeControl.device as Esp32InputDevice;
		}

		if (lastInputDevice != null) 
		{
			float value = encoder.ReadValue<float>();
			float speed = Mathf.Abs(value - lastValue) / Time.deltaTime;
			smoothSpeed = Mathf.Lerp(smoothSpeed, speed, Time.deltaTime * 12);
			lastInputDevice.SendMotorSpeed(Mathf.InverseLerp(0,2f, smoothSpeed));
			lastValue = value;
		}
	}
}

#endif