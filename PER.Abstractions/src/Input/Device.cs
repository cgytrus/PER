using System;

namespace PER.Abstractions.Input;

public abstract class Device<TSelf> : IDeviceProvider<TSelf> where TSelf : Device<TSelf> {
    public int count => 1;
    public TSelf this[int index] => index == 0 ? (TSelf)this : Array.Empty<TSelf>()[index];

    public abstract void Setup();
    public abstract void Update(TimeSpan time);
    public abstract void Finish();
}
