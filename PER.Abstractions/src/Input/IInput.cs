using System;
using System.Numerics;

using JetBrains.Annotations;

using PER.Util;

namespace PER.Abstractions.Input;

[PublicAPI]
public interface IInput : IUpdatable, ISetupable {
    [PublicAPI]
    public struct KeyDownArgs(KeyCode key, bool system, bool shift, bool control, bool alt) {
        public KeyCode key { get; } = key;
        public bool system { get; } = system;
        public bool shift { get; } = shift;
        public bool control { get; } = control;
        public bool alt { get; } = alt;
    }

    [PublicAPI]
    public struct TextEnteredArgs(string text) {
        public string text { get; } = text;
    }

    [PublicAPI]
    public struct ScrolledArgs(float delta) {
        public float delta { get; } = delta;
    }

    public bool block { get; set; }

    public Vector2Int mousePosition { get; }
    public Vector2 accurateMousePosition { get; }
    public Vector2 normalizedMousePosition { get; }

    public Vector2Int previousMousePosition { get; }
    public Vector2 previousAccurateMousePosition { get; }
    public Vector2 previousNormalizedMousePosition { get; }

    public bool keyRepeat { get; set; }

    public string clipboard { get; set; }

    public event Action<KeyDownArgs>? keyDown;
    public event Action<TextEnteredArgs>? textEntered;
    public event Action<ScrolledArgs>? scrolled;

    public void Finish();

    public bool KeyPressed(KeyCode key);
    public bool KeysPressed(KeyCode key1, KeyCode key2);
    public bool KeysPressed(KeyCode key1, KeyCode key2, KeyCode key3);
    public bool KeysPressed(KeyCode key1, KeyCode key2, KeyCode key3, KeyCode key4);
    public bool KeysPressed(params KeyCode[] keys);

    public bool MouseButtonPressed(MouseButton button);
}
