public struct ESP32DeviceInfo
{
	public string name;
	public int firmwareVersion;
	public float batteryVoltage;
	public float batteryLevel;
	public bool hasMotor;

	public override string ToString()
	{
		return $"{nameof(name)}: {name}, {nameof(firmwareVersion)}: {firmwareVersion}, {nameof(batteryLevel)}: {batteryLevel}, {nameof(hasMotor)}: {hasMotor}";
	}
}