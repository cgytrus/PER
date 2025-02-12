using JetBrains.Annotations;

namespace PER.Abstractions.Input;

[PublicAPI]
public interface IInput : IUpdatable, ISetupable {
    public void Finish();

    public int Count<TDevice>() where TDevice : Device;
    public TDevice Get<TDevice>(int index = 0) where TDevice : Device;
}
