﻿using System;
using System.Numerics;

using OpenTK.Windowing.GraphicsLibraryFramework;

using PER.Abstractions.Input;
using PER.Util;

using MouseButton = PER.Abstractions.Input.MouseButton;

namespace PRR.OpenGL;

public class InputManager : IInput {
    public bool block { get; set; }

    public Vector2Int mousePosition => block ? new Vector2Int(-1, -1) : _mousePosition;
    public Vector2 accurateMousePosition => block ? new Vector2(-1f, -1f) : _accurateMousePosition;
    public Vector2 normalizedMousePosition => block ? new Vector2(-1f, -1f) : _normalizedMousePosition;

    public Vector2Int previousMousePosition => block ? new Vector2Int(-1, -1) : _previousMousePosition;
    public Vector2 previousAccurateMousePosition => block ? new Vector2(-1f, -1f) : _previousAccurateMousePosition;
    public Vector2 previousNormalizedMousePosition => block ? new Vector2(-1f, -1f) : _previousNormalizedMousePosition;

    public bool keyRepeat { get; set; }

    public string clipboard {
        get => _renderer.window?.ClipboardString ?? "";
        set {
            if(_renderer.window is null)
                return;
            _renderer.window.ClipboardString = value;
        }
    }

    public event Action<IInput.KeyDownArgs>? keyDown;
    public event Action<IInput.TextEnteredArgs>? textEntered;
    public event Action<IInput.ScrolledArgs>? scrolled;

    private Vector2Int _mousePosition = new(-1, -1);
    private Vector2 _accurateMousePosition = new(-1f, -1f);
    private Vector2 _normalizedMousePosition = new(-1f, -1f);
    private Vector2Int _previousMousePosition = new(-1, -1);
    private Vector2 _previousAccurateMousePosition = new(-1f, -1f);
    private Vector2 _previousNormalizedMousePosition = new(-1f, -1f);

    private readonly Renderer _renderer;

    public InputManager(Renderer renderer) => _renderer = renderer;

    public void Reset() {
        if(_renderer.window is null)
            return;

        _renderer.window.KeyDown +=
            key => {
                if(key.IsRepeat && !keyRepeat)
                    return;
                keyDown?.Invoke(new IInput.KeyDownArgs(Converters.ToPerKey(key.Key), key.Command, key.Shift,
                    key.Control, key.Alt));
            };
        _renderer.window.TextInput += text => EnterText(text.AsString);

        _renderer.window.MouseMove += mouse => UpdateMousePosition(mouse.X, mouse.Y);
        _renderer.window.MouseWheel += scroll => ScrollMouse(scroll.Offset.Y);
    }

    public void Update(TimeSpan time) {
        _previousMousePosition = _mousePosition;
        _previousAccurateMousePosition = _accurateMousePosition;
        _previousNormalizedMousePosition = _normalizedMousePosition;
    }

    public void Finish() { }

    public bool KeyPressed(KeyCode key) {
        if(block || _renderer.window is null)
            return false;
        Keys otkKey = Converters.ToOtkKey(key);
        return otkKey != Keys.Unknown && _renderer.window.IsKeyDown(otkKey);
    }

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

    public bool MouseButtonPressed(MouseButton button) => !block && _renderer.window is not null &&
        _renderer.window.IsMouseButtonDown(Converters.ToOtkMouseButton(button));

    private void EnterText(string text) => textEntered?.Invoke(new IInput.TextEnteredArgs(text));

    private void UpdateMousePosition(float mouseX, float mouseY) {
        if(!_renderer.focused) {
            _mousePosition = new Vector2Int(-1, -1);
            _accurateMousePosition = new Vector2(-1f, -1f);
            _normalizedMousePosition = new Vector2(-1f, -1f);
            return;
        }

        Vector2 pixelMousePosition = new(
            mouseX - _renderer.window?.Size.X * 0.5f + _renderer.width * _renderer.font.size.x * 0.5f ?? 0f,
            mouseY - _renderer.window?.Size.Y * 0.5f + _renderer.height * _renderer.font.size.y * 0.5f ?? 0f);
        _accurateMousePosition = new Vector2(
            pixelMousePosition.X / _renderer.font.size.x,
            pixelMousePosition.Y / _renderer.font.size.y);
        _mousePosition = new Vector2Int((int)_accurateMousePosition.X, (int)_accurateMousePosition.Y);
        _normalizedMousePosition =
            new Vector2(pixelMousePosition.X / (_renderer.width * _renderer.font.size.x - 1),
                pixelMousePosition.Y / (_renderer.height * _renderer.font.size.y - 1));
    }

    private void ScrollMouse(float delta) => scrolled?.Invoke(new IInput.ScrolledArgs(delta));
}
