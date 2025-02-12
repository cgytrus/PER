using System;

namespace PER.Abstractions.Input;

public abstract class Device : IUpdatable {
    public abstract void Update(TimeSpan time);
    public abstract void Finish();
}
