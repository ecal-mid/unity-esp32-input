public struct ESP32DeviceInfo
{
	public string name;
	public int firmwareVersion;
	public float batteryVoltage;
	public float batteryLevel;
	public int motorCount;
	public int encoderCount;
	public int buttonCount;

	public override string ToString()
	{
		return $"{nameof(name)}: {name}, {nameof(firmwareVersion)}: {firmwareVersion}, {nameof(batteryLevel)}: {batteryLevel}, {nameof(motorCount)}: {motorCount}, {nameof(encoderCount)}: {encoderCount}, {nameof(buttonCount)}: {buttonCount}";
	}
}