﻿using System.Globalization;

using JetBrains.Annotations;

using PER.Abstractions;
using PER.Abstractions.Rendering;
using PER.Common.Effects;
using PER.Util;

namespace PER.Common;

[PublicAPI]
public abstract class ScreenGame : IGame {
    private const float StartupWaitTime = 0.5f;
    private const float StartupFadeTime = 2f;
    private const float ShutdownFadeTime = 2f;
    private const float FadeTime = 0.3f;

    protected abstract FrameTime? frameTime { get; }
    protected abstract IRenderer renderer { get; }

    public IScreen? currentScreen { get; private set; }
    private readonly FadeEffect _screenFade = new();

    private Func<char, Formatting> _frameTimeFormatter = _ => new Formatting(Color.white, Color.transparent);

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
            if(middleCallback is not null && !middleCallback.Invoke())
                return;
            currentScreen?.Close();
            currentScreen = screen;
            currentScreen?.Open();
            if(currentScreen is null)
                renderer.Close();
        });

    public void FadeScreen(Action middleCallback) =>
        _screenFade.Start(FadeTime, FadeTime, middleCallback);

    public void FadeScreen(float fadeOutTime, float fadeInTime, Action middleCallback) =>
        _screenFade.Start(fadeOutTime, fadeInTime, middleCallback);

    public abstract void Unload();
    public abstract void Load();
    public abstract RendererSettings Loaded();

    public virtual void Setup() {
        renderer.closed += (_, _) => SwitchScreen(null);
        _frameTimeFormatter = flag =>
            frameTime is null ? new Formatting(Color.white, Color.transparent) : FrameTimeFormatter(frameTime, flag);
    }

    public virtual void Update(TimeSpan time) {
        if(_screenFade.fading)
            renderer.AddEffect(_screenFade);

        currentScreen?.Update(time);

        if(currentScreen != null)
            DrawFrameTime();
    }

    private void DrawFrameTime() {
        if(this.frameTime is null)
            return;
        CultureInfo culture = CultureInfo.InvariantCulture;
        string fps = this.frameTime.fps.ToString("F1", culture);
        string avgFps = this.frameTime.averageFps.ToString("F1", culture);
        string frameTime = this.frameTime.frameTime.TotalMilliseconds.ToString("F2", culture);
        string avgFrameTime = this.frameTime.averageFrameTime.TotalMilliseconds.ToString("F2", culture);
        renderer.DrawText(new Vector2Int(renderer.width - 1, renderer.height - 2),
            $"\f1{frameTime}\f\0/\f2{avgFrameTime}\f\0 ms", _frameTimeFormatter, HorizontalAlignment.Right);
        renderer.DrawText(new Vector2Int(renderer.width - 1, renderer.height - 1),
            $"\fa{fps}\f\0/\fb{avgFps}\f\0 FPS", _frameTimeFormatter, HorizontalAlignment.Right);
    }

    protected virtual Formatting FrameTimeFormatter(FrameTime frameTime, char flag) =>
        new(Color.white, Color.transparent);

    public virtual void Tick(TimeSpan time) => currentScreen?.Tick(time);

    public virtual void Finish() { }
}
