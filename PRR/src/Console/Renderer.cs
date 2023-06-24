using System;
using System.IO;

using PER.Abstractions.Rendering;
using PER.Util;

using Color = PER.Util.Color;

namespace PRR.Console;

public class Renderer : BasicRenderer {
    public override bool open => _open;

    public override bool focused => true; // TODO
    public override event EventHandler? focusChanged;
    public override event EventHandler? closed;

    public override Color background {
        get => base.background;
        set {
            base.background = value;
            _background = ConsoleConverters.ToConsoleColor(value);
        }
    }

    public Text? text { get; private set; }

    private bool _open;

    private bool _swapBuffers;

    private ConsoleColor _background = ConsoleColor.Black;

    public override void Update(TimeSpan time) { }

    public override void Close() => _open = false;

    public override void Finish() {
        _open = false;
        text = null;
    }

    protected override void CreateWindow() {
        UpdateFont();

        _open = true;

        System.Console.SetWindowSize(width, height + 1);
        System.Console.SetBufferSize(width, height + 1);
        System.Console.SetWindowPosition(0, 0);
        System.Console.CursorVisible = false;

        UpdateIcon();

        System.Console.CancelKeyPress += (_, _) => closed?.Invoke(this, EventArgs.Empty);

        UpdateFramerate();
    }

    protected override void UpdateFramerate() { }
    protected override void UpdateTitle() => System.Console.Title = title;
    protected override void UpdateIcon() { }

    protected override void CreateText() =>
        text = new Text(new Vector2Int(width, height), globalModEffects, display, displayUsed, effects);

    public override void Draw() {
        DrawAllEffects();
        System.Console.BackgroundColor = _background;
        RunPipelines();
    }

    private void RunPipelines() {
        // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
        foreach(IEffect effect in globalEffects) {
            if(effect.pipeline is null)
                continue;
            foreach(PipelineStep step in effect.pipeline)
                if(step.stepType == PipelineStep.Type.Text)
                    text?.Draw();
        }
    }
}
