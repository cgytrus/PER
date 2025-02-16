using System;
using PER.Abstractions.Meta;

namespace PER.Abstractions;

public interface IUpdatable {
    [RequiresHead]
    public void Update(TimeSpan time);
}
