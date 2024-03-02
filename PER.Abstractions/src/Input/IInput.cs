using System;
using System.Numerics;

using JetBrains.Annotations;

using PER.Util;

namespace PER.Abstractions.Input;

[PublicAPI]
public interface IInput : IUpdatable, ISetupable {
    [PublicAPI]
    public struct KeyDownArgs {
        public KeyCode key { get; }
        public bool system { get; }
        public bool shift { get; }
        public bool control { get; }
        public bool alt { get; }
        public KeyDownArgs(KeyCode key, bool system, bool shift, bool control, bool alt) {
            this.key = key;
            this.system = system;
            this.shift = shift;
            this.control = control;
            this.alt = alt;
        }
    }

    [PublicAPI]
    public struct TextEnteredArgs {
        public string text { get; }
        public TextEnteredArgs(string text) => this.text = text;
    }

    [PublicAPI]
    public struct ScrolledArgs {
        public float delta { get; }
        public ScrolledArgs(float delta) => this.delta = delta;
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
