using System;

using JetBrains.Annotations;

namespace PER.Abstractions.Screens;

[PublicAPI]
public interface IScreens {
    public IScreen? currentScreen { get; }
    public void SwitchScreen(IScreen? screen, Func<bool>? middleCallback = null);
    public void SwitchScreen(IScreen? screen, float fadeOutTime, float fadeInTime, Func<bool>? middleCallback = null);
    public void FadeScreen(Action middleCallback);
    public void FadeScreen(float fadeOutTime, float fadeInTime, Action middleCallback);
}
