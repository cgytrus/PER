using System;
using PER.Abstractions.Meta;

namespace PER.Abstractions;

public interface ITickable {
    [RequiresBody]
    public void Tick(TimeSpan time);
}
