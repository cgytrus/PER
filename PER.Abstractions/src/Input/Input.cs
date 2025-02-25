using System;
using PER.Abstractions.Meta;

namespace PER.Abstractions.Input;

public class Input<T1>(IInput.IHandler handler, IDeviceProvider<T1> devices) : IInput, IInput.IImpl where T1 : IDevice {
    public virtual void Update(TimeSpan time) { handler.Input(this); devices.Update(time); }
    public virtual int Count<TDevice>() where TDevice : class, IDevice => (devices as IDeviceProvider<TDevice>)?.count ?? 0;
    public virtual TDevice Get<TDevice>(int index = 0) where TDevice : class, IDevice =>
        (devices as IDeviceProvider<TDevice>)?[index] ?? Array.Empty<TDevice>()[index];
}

public class Input<T1, T2>(
    IInput.IHandler handler,
    IDeviceProvider<T1> devices1,
    IDeviceProvider<T2> devices
) : Input<T1>(handler, devices1)
    where T1 : IDevice
    where T2 : IDevice {
    public override void Update(TimeSpan time) { base.Update(time); devices.Update(time); }
    public override int Count<TDevice>() => (devices as IDeviceProvider<TDevice>)?.count ?? base.Count<TDevice>();
    public override TDevice Get<TDevice>(int index = 0) => (devices as IDeviceProvider<TDevice>)?[index] ?? base.Get<TDevice>(index);
}

public class Input<T1, T2, T3>(
    IInput.IHandler handler,
    IDeviceProvider<T1> devices1,
    IDeviceProvider<T2> devices2,
    IDeviceProvider<T3> devices
) : Input<T1, T2>(handler, devices1, devices2)
    where T1 : IDevice
    where T2 : IDevice
    where T3 : IDevice {
    public override void Update(TimeSpan time) { base.Update(time); devices.Update(time); }
    public override int Count<TDevice>() => (devices as IDeviceProvider<TDevice>)?.count ?? base.Count<TDevice>();
    public override TDevice Get<TDevice>(int index = 0) => (devices as IDeviceProvider<TDevice>)?[index] ?? base.Get<TDevice>(index);
}

public class Input<T1, T2, T3, T4>(
    IInput.IHandler handler,
    IDeviceProvider<T1> devices1,
    IDeviceProvider<T2> devices2,
    IDeviceProvider<T3> devices3,
    IDeviceProvider<T4> devices
) : Input<T1, T2, T3>(handler, devices1, devices2, devices3)
    where T1 : IDevice
    where T2 : IDevice
    where T3 : IDevice
    where T4 : IDevice {
    public override void Update(TimeSpan time) { base.Update(time); devices.Update(time); }
    public override int Count<TDevice>() => (devices as IDeviceProvider<TDevice>)?.count ?? base.Count<TDevice>();
    public override TDevice Get<TDevice>(int index = 0) => (devices as IDeviceProvider<TDevice>)?[index] ?? base.Get<TDevice>(index);
}

public class Input<T1, T2, T3, T4, T5>(
    IInput.IHandler handler,
    IDeviceProvider<T1> devices1,
    IDeviceProvider<T2> devices2,
    IDeviceProvider<T3> devices3,
    IDeviceProvider<T4> devices4,
    IDeviceProvider<T5> devices
) : Input<T1, T2, T3, T4>(handler, devices1, devices2, devices3, devices4)
    where T1 : IDevice
    where T2 : IDevice
    where T3 : IDevice
    where T4 : IDevice
    where T5 : IDevice {
    public override void Update(TimeSpan time) { base.Update(time); devices.Update(time); }
    public override int Count<TDevice>() => (devices as IDeviceProvider<TDevice>)?.count ?? base.Count<TDevice>();
    public override TDevice Get<TDevice>(int index = 0) => (devices as IDeviceProvider<TDevice>)?[index] ?? base.Get<TDevice>(index);
}
