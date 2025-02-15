namespace PER.Abstractions.Input;

public interface IDevice : ISetupable, IUpdatable {
    public void Finish();
}
