using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

using PER.Abstractions;
using PER.Abstractions.Audio;
using PER.Abstractions.Input;
using PER.Abstractions.Rendering;
using PER.Abstractions.Resources;
using PER.Abstractions.UI;
using PER.Demo.Screens.Templates;
using PER.Util;

using PRR.UI;
using PRR.UI.Resources;

namespace PER.Demo.Screens;

public class GameScreen : LayoutResource, IScreen {
    public const string GlobalId = "layouts/game";

    protected override IRenderer renderer => Core.engine.renderer;
    protected override IInput input => Core.engine.input;
    protected override IAudio audio => Core.engine.audio;

    protected override string layoutName => "game";
    protected override IReadOnlyDictionary<string, Type> elementTypes { get; } = new Dictionary<string, Type> {
        { "testPanel", typeof(LayoutResourceFilledPanel) },
        { "testText", typeof(LayoutResourceText) },
        { "testButton1", typeof(LayoutResourceButton) },
        { "testButton2", typeof(LayoutResourceButton) },
        { "testButton3", typeof(LayoutResourceButton) },
        { "testButton4", typeof(LayoutResourceButton) },
        { "testButton5", typeof(LayoutResourceButton) },
        { "testButton6", typeof(LayoutResourceButton) },
        { "testSliderText", typeof(LayoutResourceText) },
        { "testSlider", typeof(LayoutResourceSlider) },
        { "packs", typeof(LayoutResourceListBox<ResourcePackData>) },
        { "packsButton", typeof(LayoutResourceButton) },
        { "applyButton", typeof(LayoutResourceButton) },
        { "reloadButton", typeof(LayoutResourceButton) },
        { "testProgressBar", typeof(LayoutResourceProgressBar) },
        { "testInputField", typeof(LayoutResourceInputField) }
    };

    protected override IEnumerable<KeyValuePair<string, Type>> dependencyTypes {
        get {
            foreach(KeyValuePair<string, Type> pair in base.dependencyTypes)
                yield return pair;
            yield return new KeyValuePair<string, Type>(ResourcePackSelectorTemplate.GlobalId,
                typeof(ResourcePackSelectorTemplate));
        }
    }

    private readonly Settings _settings;

    private readonly List<Func<char, Formatting>> _styleFormatters = new();

    private readonly List<ResourcePackData> _availablePacks = new();
    private readonly HashSet<ResourcePackData> _loadedPacks = new();

    private ProgressBar? _testProgressBar;

    public GameScreen(Settings settings, IResources resources) {
        _settings = settings;
        resources.TryAddResource(ResourcePackSelectorTemplate.GlobalId,
            new ResourcePackSelectorTemplate(this, _availablePacks, _loadedPacks));
    }

    public override void Load(string id) {
        base.Load(id);

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
                Core.engine.game.SwitchScreen(screen);
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
                'g' => new Formatting(Color.white, Color.transparent, RenderStyle.None, RenderOptions.Default,
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
                RenderStyle.None, RenderOptions.Default, renderer.formattingEffects["glitch"]));

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
            _testProgressBar.value = input.normalizedMousePosition.x;

        foreach((string _, Element element) in elements)
            element.Update(time);
    }

    public void Tick(TimeSpan time) { }

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
