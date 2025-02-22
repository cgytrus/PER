﻿using PER.Abstractions;
using PER.Abstractions.Meta;
using PER.Abstractions.Rendering;
using PER.Abstractions.Screens;
using PER.Common.Effects;

namespace PER.Common.Screens;

public class Screens : IScreens, ISetupable, IUpdatable, ITickable {
    private const float StartupWaitTime = 0.5f;
    private const float StartupFadeTime = 2f;
    private const float ShutdownFadeTime = 2f;
    private const float FadeTime = 0.3f;

    public IScreen? currentScreen { get; private set; }
    private readonly FadeEffect _screenFade = new();

    public void SwitchScreen(IScreen? screen, Func<bool>? middleCallback = null) {
        if(currentScreen is null)
            SwitchScreen(screen, StartupWaitTime, StartupFadeTime, middleCallback);
        else if(screen is null)
            SwitchScreen(screen, ShutdownFadeTime, 0f, middleCallback);
        else
            SwitchScreen(screen, FadeTime, FadeTime, middleCallback);
    }

    public void SwitchScreen(IScreen? screen, float fadeOutTime, float fadeInTime, Func<bool>? middleCallback = null) =>
        FadeScreen(fadeOutTime, fadeInTime, () => {
            if (middleCallback is not null && !middleCallback.Invoke())
                return;
            currentScreen?.Close();
            currentScreen = screen;
            currentScreen?.Open();
            if (currentScreen is null)
                renderer.Close();
        });

    public void FadeScreen(Action middleCallback) =>
        _screenFade.Start(FadeTime, FadeTime, middleCallback);

    public void FadeScreen(float fadeOutTime, float fadeInTime, Action middleCallback) =>
        _screenFade.Start(fadeOutTime, fadeInTime, middleCallback);

    public void Setup() => renderer.closed += (_, _) => SwitchScreen(null);

    public void Update(TimeSpan time) {
        _screenFade.Update(time);
        if(_screenFade.fading)
            renderer.AddEffect(_screenFade);
        (currentScreen as IUpdatable)?.Update(time);
    }

    public void Tick(TimeSpan time) {
        (currentScreen as ITickable)?.Tick(time);
    }
}
