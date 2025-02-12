﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace PER.Abstractions.Input;

public abstract class DeviceProvider<TDev> : ISetupable, IUpdatable where TDev : Device {
    protected List<TDev> devices { get; } = [];

    public int Count<TDevice>() => devices.OfType<TDevice>().Count();
    public TDev this[int index] => devices[index];

    public virtual void Setup() {
        foreach (TDev device in devices)
            (device as ISetupable)?.Setup();
    }

    public virtual void Update(TimeSpan time) {
        foreach (TDev device in devices)
            (device as IUpdatable)?.Update(time);
    }

    public virtual void Finish() {
        foreach (TDev device in devices)
            device.Finish();
    }
}
