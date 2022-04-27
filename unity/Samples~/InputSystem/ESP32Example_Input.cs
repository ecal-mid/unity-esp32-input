#if UNITY_INPUT_SYSTEM
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class ESP32Example_Input : MonoBehaviour
{
	public InputAction button;
	public InputAction encoder;

	void Start()
	{
		button.Enable();
		encoder.Enable();
	}

	void Update()
	{
		transform.localPosition = new Vector3(encoder.ReadValue<float>(), 0, 0);
		transform.localScale = (button.IsPressed() ? 1.5f : 1) * Vector3.one;
	}
}

#endif