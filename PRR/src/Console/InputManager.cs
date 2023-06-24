using System;
using System.Collections.Generic;
using System.Numerics;

using PER.Abstractions.Input;
using PER.Util;

namespace PRR.Console;

public class InputManager : IInput {
    public bool block { get; set; }

    public Vector2Int mousePosition => block ? new Vector2Int(-1, -1) : _mousePosition;
    public Vector2 accurateMousePosition => block ? new Vector2(-1f, -1f) : _accurateMousePosition;
    public Vector2 normalizedMousePosition => block ? new Vector2(-1f, -1f) : _normalizedMousePosition;

    public Vector2Int previousMousePosition => block ? new Vector2Int(-1, -1) : _previousMousePosition;
    public Vector2 previousAccurateMousePosition => block ? new Vector2(-1f, -1f) : _previousAccurateMousePosition;
    public Vector2 previousNormalizedMousePosition => block ? new Vector2(-1f, -1f) : _previousNormalizedMousePosition;

    public bool keyRepeat {
        get => true;
        set { }
    }

    public string clipboard {
        get => "";
        set { }
    }

    public event EventHandler<IInput.KeyDownEventArgs>? keyDown;
    public event EventHandler<IInput.TextEnteredEventArgs>? textEntered;
    public event EventHandler<IInput.ScrolledEventArgs>? scrolled;

    private Vector2Int _mousePosition = new(-1, -1);
    private Vector2 _accurateMousePosition = new(-1f, -1f);
    private Vector2 _normalizedMousePosition = new(-1f, -1f);
    private Vector2Int _previousMousePosition = new(-1, -1);
    private Vector2 _previousAccurateMousePosition = new(-1f, -1f);
    private Vector2 _previousNormalizedMousePosition = new(-1f, -1f);

    private readonly HashSet<KeyCode> _pressedKeys = new();
    private readonly HashSet<MouseButton> _pressedMouseButtons = new();

    private readonly Renderer _renderer;

    public InputManager(Renderer renderer) => _renderer = renderer;

    public void Reset() {
        //_renderer.window.KeyPressed += (_, key) => UpdateKeyPressed(ConsoleConverters.ToPerKey(key.Code), true);
        //_renderer.window.KeyReleased += (_, key) => UpdateKeyPressed(ConsoleConverters.ToPerKey(key.Code), false);
        //_renderer.window.TextEntered += (_, text) => EnterText(text.Unicode);

        //_renderer.window.MouseButtonPressed += (_, button) =>
        //    UpdateMouseButtonPressed(ConsoleConverters.ToPerMouseButton(button.Button), true);
        //_renderer.window.MouseButtonReleased += (_, button) =>
        //    UpdateMouseButtonPressed(ConsoleConverters.ToPerMouseButton(button.Button), false);

        //_renderer.window.MouseMoved += (_, mouse) => UpdateMousePosition(mouse.X, mouse.Y);
        //_renderer.window.MouseWheelScrolled += (_, scroll) => ScrollMouse(scroll.Delta);
    }

    public void Update(TimeSpan time) {
        _previousMousePosition = _mousePosition;
        _previousAccurateMousePosition = _accurateMousePosition;
        _previousNormalizedMousePosition = _normalizedMousePosition;

        _pressedKeys.Clear();
        _pressedMouseButtons.Clear();

        while(System.Console.KeyAvailable) {
            ConsoleKeyInfo key = System.Console.ReadKey(true);
            HandleInput(key);
        }
    }

    private void HandleInput(ConsoleKeyInfo key) {
        if((key.Modifiers & ConsoleModifiers.Control) != 0) {
            switch(key.Key) {
                case ConsoleKey.LeftArrow:
                    UpdateMousePosition(_mousePosition.x - 1, _mousePosition.y);
                    return;
                case ConsoleKey.RightArrow:
                    UpdateMousePosition(_mousePosition.x + 1, _mousePosition.y);
                    return;
                case ConsoleKey.UpArrow:
                    UpdateMousePosition(_mousePosition.x, _mousePosition.y - 1);
                    return;
                case ConsoleKey.DownArrow:
                    UpdateMousePosition(_mousePosition.x, _mousePosition.y + 1);
                    return;
                case ConsoleKey.PageUp:
                    ScrollMouse(3f);
                    return;
                case ConsoleKey.PageDown:
                    ScrollMouse(-3f);
                    return;
                case ConsoleKey.OemMinus:
                    UpdateMouseButtonPressed(MouseButton.Left, true);
                    return;
                case ConsoleKey.OemPlus:
                    UpdateMouseButtonPressed(MouseButton.Right, true);
                    return;
            }
        }

        KeyCode perKey = ConsoleConverters.ToPerKey(key.Key);
        if(perKey == KeyCode.Unknown)
            return;
        if((key.Modifiers & ConsoleModifiers.Alt) != 0)
            UpdateKeyPressed(KeyCode.LAlt, true);
        if((key.Modifiers & ConsoleModifiers.Shift) != 0)
            UpdateKeyPressed(KeyCode.LShift, true);
        if((key.Modifiers & ConsoleModifiers.Control) != 0)
            UpdateKeyPressed(KeyCode.LControl, true);
        UpdateKeyPressed(ConsoleConverters.ToPerKey(key.Key), true);
        EnterText(key.KeyChar.ToString());
    }

    public void Finish() { }

    public bool KeyPressed(KeyCode key) => !block && _pressedKeys.Contains(key);

    public bool KeysPressed(KeyCode key1, KeyCode key2) => !block && KeyPressed(key1) && KeyPressed(key2);

    public bool KeysPressed(KeyCode key1, KeyCode key2, KeyCode key3) =>
        !block && KeyPressed(key1) && KeyPressed(key2) && KeyPressed(key3);

    public bool KeysPressed(KeyCode key1, KeyCode key2, KeyCode key3, KeyCode key4) =>
        !block && KeyPressed(key1) && KeyPressed(key2) && KeyPressed(key3) && KeyPressed(key4);

    public bool KeysPressed(params KeyCode[] keys) {
        if(block)
            return false;

        // ReSharper disable once LoopCanBeConvertedToQuery
        foreach(KeyCode key in keys)
            if(!KeyPressed(key))
                return false;

        return true;
    }

    public bool MouseButtonPressed(MouseButton button) => !block && _pressedMouseButtons.Contains(button);

    private void UpdateKeyPressed(KeyCode key, bool pressed) {
        if(pressed) {
            _pressedKeys.Add(key);
            keyDown?.Invoke(this, new IInput.KeyDownEventArgs(key));
        }
        else _pressedKeys.Remove(key);
    }

    private void EnterText(string text) => textEntered?.Invoke(this, new IInput.TextEnteredEventArgs(text));

    private void UpdateMouseButtonPressed(MouseButton button, bool pressed) {
        if(pressed) _pressedMouseButtons.Add(button);
        else _pressedMouseButtons.Remove(button);
    }

    private void UpdateMousePosition(int mouseX, int mouseY) {
        if(!_renderer.focused) {
            _mousePosition = new Vector2Int(-1, -1);
            _accurateMousePosition = new Vector2(-1f, -1f);
            _normalizedMousePosition = new Vector2(-1f, -1f);
            return;
        }

        _accurateMousePosition = new Vector2(mouseX, mouseY);
        _mousePosition = new Vector2Int(mouseX, mouseY);
        _normalizedMousePosition = new Vector2(mouseX, mouseY);
        _normalizedMousePosition = new Vector2(_mousePosition.x / (_renderer.width - 1f),
            _mousePosition.y / (_renderer.height - 1f));
    }

    private void ScrollMouse(float delta) => scrolled?.Invoke(this, new IInput.ScrolledEventArgs(delta));
}
