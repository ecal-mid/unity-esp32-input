public struct Esp32Event<T> where T:struct
{
	public string senderAddress;
	public T data;
}