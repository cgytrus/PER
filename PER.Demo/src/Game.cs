using System;

using PER.Abstractions;
using PER.Abstractions.Meta;
using PER.Abstractions.Rendering;
using PER.Abstractions.Resources;
using PER.Common;
using PER.Common.Effects;
using PER.Common.Resources;
using PER.Demo.Resources;
using PER.Demo.Screens;
using PER.Util;

namespace PER.Demo;

public class Game : IGame, ISetupable, IUpdatable {
    private const string SettingsPath = "config.json";
    private Settings _settings = new();

    private static TimeSpan fpsGood => (Engine.updateInterval > TimeSpan.Zero ? Engine.updateInterval :
        TimeSpan.FromSeconds(1d / 60d)) + TimeSpan.FromSeconds(0.001d);
    private static TimeSpan fpsOk => (Engine.updateInterval > TimeSpan.Zero ? Engine.updateInterval * 2 :
        TimeSpan.FromSeconds(1d / 60d) * 2) + TimeSpan.FromSeconds(0.001d);

    private Color _fpsGoodColor;
    private Color _fpsOkColor;
    private Color _fpsBadColor;

    private FrameTimeDisplay? _frameTimeDisplay;

    public void Unload() => _settings.Save(SettingsPath);

    [RequiresBody]
    public void Load() {
        _settings = Settings.Load(SettingsPath);

        resources.TryAddPacksByNames(_settings.packs);

        resources.TryAddResource("audio", new AudioResources());
        resources.TryAddResource(FontResource.GlobalId, new FontResource());
        resources.TryAddResource(ColorsResource.GlobalId, new ColorsResource());

        renderer.formattingEffects.Clear();
        renderer.formattingEffects.Add("none", null);
        renderer.formattingEffects.Add("glitch", new GlitchEffect());

        resources.TryAddResource(GameScreen.GlobalId, new GameScreen(_settings));
    }

    [RequiresBody, RequiresHead]
    public void Loaded() {
        if (!resources.TryGetResource(FontResource.GlobalId, out FontResource? font) || font.font is null)
            throw new InvalidOperationException("Missing font.");
        resources.TryGetResource(IconResource.GlobalId, out IconResource? icon);

        if (!resources.TryGetResource(ColorsResource.GlobalId, out ColorsResource? colors) ||
            !colors.colors.TryGetValue("background", out Color backgroundColor))
            throw new InvalidOperationException("Missing colors or background color.");
        renderer.background = backgroundColor;
        if (!colors.colors.TryGetValue("fps_good", out _fpsGoodColor))
            _fpsGoodColor = Color.white;
        if  (!colors.colors.TryGetValue("fps_ok", out _fpsOkColor))
            _fpsOkColor = Color.white;
        if (!colors.colors.TryGetValue("fps_bad", out _fpsBadColor))
            _fpsBadColor = Color.white;

        _settings.Apply();

        Engine.rendererSettings = new RendererSettings {
            fullscreen = false,
            font = font.font,
            icon = icon?.icon
        };
        renderer.verticalSync = false;
    }

    [RequiresBody, RequiresHead]
    public void Setup() {
        _frameTimeDisplay = new FrameTimeDisplay(Engine.frameTime, FrameTimeFormatter);
        if(!resources.TryGetResource(GameScreen.GlobalId, out GameScreen? screen))
            return;
        screens.SwitchScreen(screen);
    }

    [RequiresHead]
    public void Update(TimeSpan time) {
        _frameTimeDisplay?.Update(time);
    }

    public void Finish() { }

    private Formatting FrameTimeFormatter(FrameTime frameTime, char flag) => flag switch {
        '1' or 'a' => new Formatting(frameTime.frameTime > fpsOk ? _fpsBadColor :
            frameTime.frameTime > fpsGood ? _fpsOkColor : _fpsGoodColor, Color.transparent),
        '2' or 'b' => new Formatting(frameTime.averageFrameTime > fpsOk ? _fpsBadColor :
            frameTime.averageFrameTime > fpsGood ? _fpsOkColor : _fpsGoodColor, Color.transparent),
        _ => new Formatting(Color.white, Color.transparent)
    };
}
