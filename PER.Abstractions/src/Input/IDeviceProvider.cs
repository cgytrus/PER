namespace PER.Abstractions.Input;

public interface IDeviceProvider<out TDevice> : IDevice where TDevice : IDevice {
    public int count { get; }
    public TDevice this[int index] { get; }
}
