using System;
using System.Collections.Generic;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using PER.Abstractions.Input;
using PER.Graphics.OpenGL;

namespace PER.Input.Glfw;

public class Keyboard : Keyboard<Keyboard>, IDisposable {
    private NativeWindow window => _renderer.window;

    private readonly Dictionary<(KeyCode, ModifierKey, bool), int> _downKeys = [];
    private readonly List<string> _textInputs = [];

    protected override bool ProcKey(KeyCode key) {
        Keys otkKey = Converters.ToOtkKey(key);
        return otkKey != Keys.Unknown && window.IsKeyDown(otkKey);
    }

    protected override int ProcKeyDown((KeyCode, ModifierKey, bool) data) =>
        _downKeys.GetValueOrDefault((data.Item1, data.Item2, false), 0) +
        (data.Item3 ? _downKeys.GetValueOrDefault((data.Item1, data.Item2, true), 0) : 0);

    protected override IEnumerable<string> ProcText() => _textInputs;

    private readonly Renderer _renderer;

    public Keyboard(Renderer renderer) {
        _renderer = renderer;
        window.KeyDown += OnKeyDown;
        window.TextInput += OnTextInput;
    }

    public void Dispose() {
        window.KeyDown -= OnKeyDown;
        window.TextInput -= OnTextInput;
        GC.SuppressFinalize(this);
    }

    private readonly List<KeyboardKeyEventArgs> _keyDownEvents = [];
    private readonly List<TextInputEventArgs> _textInputEvents = [];

    private void OnKeyDown(KeyboardKeyEventArgs e) => _keyDownEvents.Add(e);
    private void OnTextInput(TextInputEventArgs e) => _textInputEvents.Add(e);

    public override void Update(TimeSpan time) {
        base.Update(time);

        _downKeys.Clear();
        foreach (KeyboardKeyEventArgs key in _keyDownEvents) {
            // what the fuck did i write
            ModifierKey mod =
                (key.Command ? ModifierKey.Cmd : ModifierKey.None) |
                (key.Shift ? ModifierKey.Shift : ModifierKey.None) |
                (key.Control ? ModifierKey.Ctrl : ModifierKey.None) |
                (key.Alt ? ModifierKey.Alt : ModifierKey.None);
            (KeyCode, ModifierKey, bool) x = (Converters.ToPerKey(key.Key), mod, key.IsRepeat);
            if (!_downKeys.TryAdd(x, 1))
                _downKeys[x]++;
        }

        _textInputs.Clear();
        foreach (TextInputEventArgs text in _textInputEvents) {
            _textInputs.Add(text.AsString);
        }

        _keyDownEvents.Clear();
        _textInputEvents.Clear();
    }
}
