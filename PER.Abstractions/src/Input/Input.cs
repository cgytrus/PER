using System;

namespace PER.Abstractions.Input;

public class Input<T1>(DeviceProvider<T1> devices) : IInput where T1 : Device {
    public virtual void Setup() => devices.Setup();
    public virtual void Update(TimeSpan time) => devices.Update(time);
    public virtual void Finish() => devices.Finish();
    public virtual int Count<TDevice>() where TDevice : Device => devices.Count<TDevice>();
    public virtual TDevice Get<TDevice>(int index = 0) where TDevice : Device => devices[index] as TDevice ?? throw new InvalidOperationException();
}

public class Input<T1, T2>(
    DeviceProvider<T1> devices1,
    DeviceProvider<T2> devices
) : Input<T1>(devices1)
    where T1 : Device
    where T2 : Device {
    public override void Setup() { base.Setup(); devices.Setup(); }
    public override void Update(TimeSpan time) { base.Update(time); devices.Update(time); }
    public override void Finish() { base.Finish(); devices.Finish(); }

    // TODO: make this stupid thing not broken
    public override int Count<TDevice>() => devices.Count<TDevice>() + base.Count<TDevice>();
    public override TDevice Get<TDevice>(int index = 0) => devices[index] as TDevice ?? base.Get<TDevice>(index);
}

public class Input<T1, T2, T3>(
    DeviceProvider<T1> devices1,
    DeviceProvider<T2> devices2,
    DeviceProvider<T3> devices
) : Input<T1, T2>(devices1, devices2)
    where T1 : Device
    where T2 : Device
    where T3 : Device {
    public override void Setup() { base.Setup(); devices.Setup(); }
    public override void Update(TimeSpan time) { base.Update(time); devices.Update(time); }
    public override void Finish() { base.Finish(); devices.Finish(); }
    public override int Count<TDevice>() => devices.Count<TDevice>() + base.Count<TDevice>();
    public override TDevice Get<TDevice>(int index = 0) => devices[index] as TDevice ?? base.Get<TDevice>(index);
}

public class Input<T1, T2, T3, T4>(
    DeviceProvider<T1> devices1,
    DeviceProvider<T2> devices2,
    DeviceProvider<T3> devices3,
    DeviceProvider<T4> devices
) : Input<T1, T2, T3>(devices1, devices2, devices3)
    where T1 : Device
    where T2 : Device
    where T3 : Device
    where T4 : Device {
    public override void Setup() { base.Setup(); devices.Setup(); }
    public override void Update(TimeSpan time) { base.Update(time); devices.Update(time); }
    public override void Finish() { base.Finish(); devices.Finish(); }
    public override int Count<TDevice>() => devices.Count<TDevice>() + base.Count<TDevice>();
    public override TDevice Get<TDevice>(int index = 0) => devices[index] as TDevice ?? base.Get<TDevice>(index);
}

public class Input<T1, T2, T3, T4, T5>(
    DeviceProvider<T1> devices1,
    DeviceProvider<T2> devices2,
    DeviceProvider<T3> devices3,
    DeviceProvider<T4> devices4,
    DeviceProvider<T5> devices
) : Input<T1, T2, T3, T4>(devices1, devices2, devices3, devices4)
    where T1 : Device
    where T2 : Device
    where T3 : Device
    where T4 : Device
    where T5 : Device {
    public override void Setup() { base.Setup(); devices.Setup(); }
    public override void Update(TimeSpan time) { base.Update(time); devices.Update(time); }
    public override void Finish() { base.Finish(); devices.Finish(); }
    public override int Count<TDevice>() => devices.Count<TDevice>() + base.Count<TDevice>();
    public override TDevice Get<TDevice>(int index = 0) => devices[index] as TDevice ?? base.Get<TDevice>(index);
}
