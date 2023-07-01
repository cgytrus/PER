using System;

using PER.Abstractions;
using PER.Abstractions.Rendering;
using PER.Abstractions.Resources;
using PER.Common;
using PER.Common.Effects;
using PER.Common.Resources;
using PER.Demo.Resources;
using PER.Demo.Screens;
using PER.Util;

using PRR.Resources;

namespace PER.Demo;

public class Game : IGame, ISetupable, IUpdatable {
    private const string SettingsPath = "config.json";
    private Settings _settings = new();

    private DrawTextEffect? _drawTextEffect;
    private BloomEffect? _bloomEffect;

    private static IRenderer renderer => Core.engine.renderer;

    private static TimeSpan fpsGood => (Core.engine.updateInterval > TimeSpan.Zero ? Core.engine.updateInterval :
        TimeSpan.FromSeconds(1d / 60d)) + TimeSpan.FromSeconds(0.001d);
    private static TimeSpan fpsOk => (Core.engine.updateInterval > TimeSpan.Zero ? Core.engine.updateInterval * 2 :
        TimeSpan.FromSeconds(1d / 60d) * 2) + TimeSpan.FromSeconds(0.001d);

    private Color _fpsGoodColor;
    private Color _fpsOkColor;
    private Color _fpsBadColor;

    private FrameTimeDisplay? _frameTimeDisplay;

    public void Unload() => _settings.Save(SettingsPath);

    public void Load() {
        IResources resources = Core.engine.resources;

        _settings = Settings.Load(SettingsPath);

        resources.TryAddPacksByNames(_settings.packs);

        resources.TryAddResource("audio", new AudioResources());
        resources.TryAddResource(FontResource.GlobalId, new FontResource());
        resources.TryAddResource(ColorsResource.GlobalId, new ColorsResource());

        _drawTextEffect = new DrawTextEffect();
        resources.TryAddResource(BloomEffect.GlobalId, new BloomEffect());

        renderer.formattingEffects.Clear();
        renderer.formattingEffects.Add("none", null);
        renderer.formattingEffects.Add("glitch", new GlitchEffect(renderer));

        resources.TryAddResource(GameScreen.GlobalId, new GameScreen(_settings, resources));
    }

    public RendererSettings Loaded() {
        if(!Core.engine.resources.TryGetResource(FontResource.GlobalId, out FontResource? font) || font.font is null)
            throw new InvalidOperationException("Missing font.");
        Core.engine.resources.TryGetResource(IconResource.GlobalId, out IconResource? icon);

        if(!Core.engine.resources.TryGetResource(ColorsResource.GlobalId, out ColorsResource? colors) ||
            !colors.colors.TryGetValue("background", out Color backgroundColor))
            throw new InvalidOperationException("Missing colors or background color.");
        renderer.background = backgroundColor;
        if(!colors.colors.TryGetValue("fps_good", out _fpsGoodColor))
            _fpsGoodColor = Color.white;
        if(!colors.colors.TryGetValue("fps_ok", out _fpsOkColor))
            _fpsOkColor = Color.white;
        if(!colors.colors.TryGetValue("fps_bad", out _fpsBadColor))
            _fpsBadColor = Color.white;

        Core.engine.resources.TryGetResource(BloomEffect.GlobalId, out _bloomEffect);

        _settings.Apply();

        return new RendererSettings {
            title = "PER Demo Pog",
            width = 80,
            height = 60,
            verticalSync = true,
            fullscreen = false,
            font = font.font,
            icon = icon?.icon
        };
    }

    public void Setup() {
        _frameTimeDisplay = new FrameTimeDisplay(Core.engine.frameTime, renderer, FrameTimeFormatter);
        if(!Core.engine.resources.TryGetResource(GameScreen.GlobalId, out GameScreen? screen))
            return;
        Core.engine.screens.SwitchScreen(screen);
    }

    public void Update(TimeSpan time) {
        if(_drawTextEffect is null || _bloomEffect is null)
            return;
        renderer.AddEffect(_drawTextEffect);
        renderer.AddEffect(_bloomEffect);
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
