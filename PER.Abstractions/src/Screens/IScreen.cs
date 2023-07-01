using JetBrains.Annotations;

namespace PER.Abstractions.Screens;

[PublicAPI]
public interface IScreen {
    public void Open();
    public void Close();
}
