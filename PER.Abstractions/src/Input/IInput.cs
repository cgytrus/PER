using JetBrains.Annotations;
using PER.Abstractions.Meta;

namespace PER.Abstractions.Input;

[PublicAPI, RequiresHead]
public interface IInput : IUpdatable, ISetupable {
    public void Finish();

    public int Count<TDevice>() where TDevice : class, IDevice;
    public TDevice Get<TDevice>(int index = 0) where TDevice : class, IDevice;
}
