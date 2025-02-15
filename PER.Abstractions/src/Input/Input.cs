using System;

namespace PER.Abstractions.Input;

public class Input<T1>(IDeviceProvider<T1> devices) : IInput where T1 : IDevice {
    public virtual void Setup() => devices.Setup();
    public virtual void Update(TimeSpan time) => devices.Update(time);
    public virtual void Finish() => devices.Finish();
    public virtual int Count<TDevice>() where TDevice : class, IDevice => (devices as IDeviceProvider<TDevice>)?.count ?? 0;
    public virtual TDevice Get<TDevice>(int index = 0) where TDevice : class, IDevice =>
        (devices as IDeviceProvider<TDevice>)?[index] ?? Array.Empty<TDevice>()[index];
}

public class Input<T1, T2>(
    IDeviceProvider<T1> devices1,
    IDeviceProvider<T2> devices
) : Input<T1>(devices1)
    where T1 : IDevice
    where T2 : IDevice {
    public override void Setup() { base.Setup(); devices.Setup(); }
    public override void Update(TimeSpan time) { base.Update(time); devices.Update(time); }
    public override void Finish() { base.Finish(); devices.Finish(); }
    public override int Count<TDevice>() => (devices as IDeviceProvider<TDevice>)?.count ?? base.Count<TDevice>();
    public override TDevice Get<TDevice>(int index = 0) => (devices as IDeviceProvider<TDevice>)?[index] ?? base.Get<TDevice>(index);
}

public class Input<T1, T2, T3>(
    IDeviceProvider<T1> devices1,
    IDeviceProvider<T2> devices2,
    IDeviceProvider<T3> devices
) : Input<T1, T2>(devices1, devices2)
    where T1 : IDevice
    where T2 : IDevice
    where T3 : IDevice {
    public override void Setup() { base.Setup(); devices.Setup(); }
    public override void Update(TimeSpan time) { base.Update(time); devices.Update(time); }
    public override void Finish() { base.Finish(); devices.Finish(); }
    public override int Count<TDevice>() => (devices as IDeviceProvider<TDevice>)?.count ?? base.Count<TDevice>();
    public override TDevice Get<TDevice>(int index = 0) => (devices as IDeviceProvider<TDevice>)?[index] ?? base.Get<TDevice>(index);
}

public class Input<T1, T2, T3, T4>(
    IDeviceProvider<T1> devices1,
    IDeviceProvider<T2> devices2,
    IDeviceProvider<T3> devices3,
    IDeviceProvider<T4> devices
) : Input<T1, T2, T3>(devices1, devices2, devices3)
    where T1 : IDevice
    where T2 : IDevice
    where T3 : IDevice
    where T4 : IDevice {
    public override void Setup() { base.Setup(); devices.Setup(); }
    public override void Update(TimeSpan time) { base.Update(time); devices.Update(time); }
    public override void Finish() { base.Finish(); devices.Finish(); }
    public override int Count<TDevice>() => (devices as IDeviceProvider<TDevice>)?.count ?? base.Count<TDevice>();
    public override TDevice Get<TDevice>(int index = 0) => (devices as IDeviceProvider<TDevice>)?[index] ?? base.Get<TDevice>(index);
}

public class Input<T1, T2, T3, T4, T5>(
    IDeviceProvider<T1> devices1,
    IDeviceProvider<T2> devices2,
    IDeviceProvider<T3> devices3,
    IDeviceProvider<T4> devices4,
    IDeviceProvider<T5> devices
) : Input<T1, T2, T3, T4>(devices1, devices2, devices3, devices4)
    where T1 : IDevice
    where T2 : IDevice
    where T3 : IDevice
    where T4 : IDevice
    where T5 : IDevice {
    public override void Setup() { base.Setup(); devices.Setup(); }
    public override void Update(TimeSpan time) { base.Update(time); devices.Update(time); }
    public override void Finish() { base.Finish(); devices.Finish(); }
    public override int Count<TDevice>() => (devices as IDeviceProvider<TDevice>)?.count ?? base.Count<TDevice>();
    public override TDevice Get<TDevice>(int index = 0) => (devices as IDeviceProvider<TDevice>)?[index] ?? base.Get<TDevice>(index);
}
