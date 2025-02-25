using System;
using System.Collections.Generic;
using System.Linq;

namespace PER.Abstractions.Input;

public abstract class DeviceProvider<TDev> : IDeviceProvider<TDev> where TDev : Device<TDev> {
    protected List<TDev> devices { get; } = [];

    public int count => devices.Count;
    public TDev this[int index] => devices[index];

    public virtual void Update(TimeSpan time) {
        foreach (TDev device in devices)
            device.Update(time);
    }
}
