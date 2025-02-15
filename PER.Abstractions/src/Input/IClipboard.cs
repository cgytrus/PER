namespace PER.Abstractions.Input;

public interface IClipboard : IDevice {
    public string value { get; set; }
}
