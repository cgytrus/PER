using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

using PER.Abstractions;
using PER.Abstractions.Audio;
using PER.Abstractions.Environment;
using PER.Abstractions.Input;
using PER.Abstractions.Rendering;
using PER.Abstractions.Resources;
using PER.Abstractions.Screens;
using PER.Abstractions.UI;
using PER.Demo.Environment;
using PER.Demo.Screens.Templates;
using PER.Util;

using PRR.UI;
using PRR.UI.Resources;

namespace PER.Demo.Screens;

public class GameScreen : LayoutResource, IScreen, IUpdatable, ITickable {
    public const string GlobalId = "layouts/game";

    protected override IRenderer renderer => Core.engine.renderer;
    protected override IInput input => Core.engine.input;
    protected override IAudio audio => Core.engine.audio;

    protected override string layoutName => "game";

    private readonly Settings _settings;

    private readonly List<Func<char, Formatting>> _styleFormatters = new();

    private readonly List<ResourcePackData> _availablePacks = new();
    private readonly HashSet<ResourcePackData> _loadedPacks = new();

    private ProgressBar? _testProgressBar;

    private readonly IResources _resources;
    private Level? _level;

    public GameScreen(Settings settings, IResources resources) {
        _settings = settings;
        _resources = resources;
        resources.TryAddResource(ResourcePackSelectorTemplate.GlobalId,
            new ResourcePackSelectorTemplate(this, _availablePacks, _loadedPacks));
    }

    public override void Preload() {
        base.Preload();
        AddDependency<ResourcePackSelectorTemplate>(ResourcePackSelectorTemplate.GlobalId);

        AddElement<FilledPanel>("testPanel");
        AddElement<Text>("testText");
        AddElement<Button>("testButton1");
        AddElement<Button>("testButton2");
        AddElement<Button>("testButton3");
        AddElement<Button>("testButton4");
        AddElement<Button>("testButton5");
        AddElement<Button>("testButton6");
        AddElement<Text>("testSliderText");
        AddElement<Slider>("testSlider");
        AddElement<ListBox<ResourcePackData>>("packs");
        AddElement<Button>("packsButton");
        AddElement<Button>("applyButton");
        AddElement<Button>("reloadButton");
        AddElement<ProgressBar>("testProgressBar");
        AddElement<InputField>("testInputField");
    }

    public override void Load(string id) {
        base.Load(id);

        _level = new Level(renderer, input, audio, _resources, new Vector2Int(16, 16));

        for(int y = -20; y <= 20; y++) {
            for(int x = -20; x <= 20; x++) {
                _level.Add(new FloorObject { position = new Vector2Int(x, y) });
            }
        }

        _level.Add(new PlayerObject());

        for(int i = -5; i <= 5; i++) {
            _level.Add(new WallObject { position = new Vector2Int(i, -5) });
        }
        for(int i = 0; i < 100; i++) {
            _level.Add(new WallObject { position = new Vector2Int(i, -8) });
        }
        for(int i = 0; i < 30; i++) {
            _level.Add(new WallObject { position = new Vector2Int(8, i - 7) });
        }

        _level.Add(new EffectObject {
            position = new Vector2Int(3, -10),
            size = new Vector2Int(10, 10),
            effect = renderer.formattingEffects["glitch"]
        });
        _level.Add(new EffectObject {
            position = new Vector2Int(-12, -24),
            size = new Vector2Int(6, 9),
            effect = renderer.formattingEffects["glitch"]
        });

        for(RenderStyle style = RenderStyle.None; style <= RenderStyle.All; style++) {
            RenderStyle curStyle = style;
            _styleFormatters.Add(_ => new Formatting(Color.white, Color.transparent, curStyle));
        }

        FilledPanel testPanel = GetElement<FilledPanel>("testPanel");
        Button testButton1 = GetElement<Button>("testButton1");
        testButton1.onClick += (_, _) => {
            testButton1.toggled = !testButton1.toggled;
            testPanel.enabled = testButton1.toggled;
        };

        Button testButton2 = GetElement<Button>("testButton2");
        int counter = 0;
        testButton2.onClick += (_, _) => {
            counter++;
            testButton2.text = counter.ToString(CultureInfo.InvariantCulture);
        };

        GetElement<Button>("testButton6").effect = renderer.formattingEffects["glitch"];

        Button packsButton = GetElement<Button>("packsButton");
        packsButton.onClick += (_, _) => {
            packsButton.toggled = !packsButton.toggled;
            if(packsButton.toggled)
                OpenPacks();
            else
                ClearPacksList();
        };

        GetElement<Button>("applyButton").onClick += (_, _) => {
            _settings.Apply();
        };

        GetElement<Button>("reloadButton").onClick += (_, _) => {
            Core.engine.Reload();
            if(Core.engine.resources.TryGetResource(GlobalId, out GameScreen? screen))
                Core.engine.screens.SwitchScreen(screen);
        };

        Text testSliderText = GetElement<Text>("testSliderText");
        Slider testSlider = GetElement<Slider>("testSlider");
        testSlider.onValueChanged += (_, _) => {
            testSliderText.text = testSlider.value.ToString(CultureInfo.InvariantCulture);
            _settings.volume = testSlider.value;
        };
        testSlider.value = _settings.volume;

        _testProgressBar = GetElement<ProgressBar>("testProgressBar");
    }

    public void Open() { }
    public void Close() { }

    public void Update(TimeSpan time) {
        _level?.Update(time);

        if(input.KeyPressed(KeyCode.F))
            return;

        renderer.DrawText(new Vector2Int(0, 0),
            """
                hello everyone! this is cConfiG  and today i'm gonna show you my gengine !!
                as you can see wit works!!1!
                tthanks for watching  everyone, shit like, subscribe, good luck, bbye!!
                """,
            flag => flag switch {
                'c' => new Formatting(new Color(0f, 1f, 0f, 1f), Color.transparent),
                'g' => new Formatting(Color.white, Color.transparent, RenderStyle.None,
                    renderer.formattingEffects["glitch"]),
                'w' => new Formatting(Color.white, Color.transparent,
                    RenderStyle.Bold | RenderStyle.Italic | RenderStyle.Underline),
                't' => new Formatting(Color.black, new Color(1f, 0f, 0f, 1f)),
                's' => new Formatting(Color.white, Color.transparent, RenderStyle.Underline),
                'b' => new Formatting(Color.white, Color.transparent, RenderStyle.Underline | RenderStyle.Bold),
                _ => new Formatting(Color.white, Color.transparent)
            });

        renderer.DrawText(new Vector2Int(0, 3),
            "more test", _ => new Formatting(Color.black, new Color(0f, 1f, 0f, 1f)));

        renderer.DrawText(new Vector2Int(0, 4),
            "\fieven more\f\0 test", flag => flag switch {
                'i' => new Formatting(new Color(1f, 0f, 1f, 0.5f), new Color(0f, 1f, 0f, 1f), RenderStyle.Italic),
                _ => new Formatting(new Color(1f, 0f, 1f, 0.5f), new Color(0f, 1f, 0f, 1f))
            });

        renderer.DrawText(new Vector2Int(10, 3),
            "per-text effects test", _ => new Formatting(Color.white, Color.transparent,
                RenderStyle.None, renderer.formattingEffects["glitch"]));

        for(int i = 0; i < _styleFormatters.Count; i++)
            renderer.DrawText(new Vector2Int(0, 5 + i), "styles test", _styleFormatters[i]);

        renderer.DrawText(new Vector2Int(39, 5),
            "left test even", _ => new Formatting(Color.white, Color.transparent));
        renderer.DrawText(new Vector2Int(39, 6),
            "left test odd", _ => new Formatting(Color.white, Color.transparent));
        renderer.DrawText(new Vector2Int(39, 7),
            "middle test even", _ => new Formatting(Color.white, Color.transparent), HorizontalAlignment.Middle);
        renderer.DrawText(new Vector2Int(39, 8),
            "middle test odd", _ => new Formatting(Color.white, Color.transparent), HorizontalAlignment.Middle);
        renderer.DrawText(new Vector2Int(39, 9),
            "-right test even", _ => new Formatting(Color.white, Color.transparent), HorizontalAlignment.Right);
        renderer.DrawText(new Vector2Int(39, 10),
            "-right test odd", _ => new Formatting(Color.white, Color.transparent), HorizontalAlignment.Right);

        if(_testProgressBar is not null &&
            input.mousePosition.InBounds(_testProgressBar.bounds) &&
            input.MouseButtonPressed(MouseButton.Left))
            _testProgressBar.value = input.normalizedMousePosition.X;

        // ReSharper disable once ForCanBeConvertedToForeach
        for(int i = 0; i < elementList.Count; i++)
            elementList[i].Update(time);
    }

    public void Tick(TimeSpan time) => _level?.Tick(time);

    private void OpenPacks() {
        _loadedPacks.Clear();
        _availablePacks.Clear();

        foreach(ResourcePackData data in Core.engine.resources.loadedPacks)
            _loadedPacks.Add(data);

        _availablePacks.AddRange(_loadedPacks);
        _availablePacks.AddRange(Core.engine.resources.GetUnloadedAvailablePacks().Reverse());

        GeneratePacksList();
    }

    public void UpdatePacks() {
        _settings.packs = _availablePacks.Where(_loadedPacks.Contains).Select(packData => packData.name).ToArray();
        GeneratePacksList();
    }

    private void GeneratePacksList() {
        ListBox<ResourcePackData> packs = GetElement<ListBox<ResourcePackData>>("packs");
        packs.Clear();
        foreach(ResourcePackData item in _availablePacks)
            packs.Add(item);
    }
    private void ClearPacksList() => GetElement<ListBox<ResourcePackData>>("packs").Clear();
}
