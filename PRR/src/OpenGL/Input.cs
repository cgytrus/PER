﻿using System;
using System.Numerics;

using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;

using PER.Abstractions.Input;
using PER.Util;

using MouseButton = PER.Abstractions.Input.MouseButton;

namespace PRR.OpenGL;

public class Input(Renderer renderer) : IInput {
    public bool block { get; set; }

    public Vector2Int mousePosition => block ? new Vector2Int(-1, -1) : _mousePosition;
    public Vector2 accurateMousePosition => block ? new Vector2(-1f, -1f) : _accurateMousePosition;
    public Vector2 normalizedMousePosition => block ? new Vector2(-1f, -1f) : _normalizedMousePosition;

    public Vector2Int previousMousePosition => block ? new Vector2Int(-1, -1) : _previousMousePosition;
    public Vector2 previousAccurateMousePosition => block ? new Vector2(-1f, -1f) : _previousAccurateMousePosition;
    public Vector2 previousNormalizedMousePosition => block ? new Vector2(-1f, -1f) : _previousNormalizedMousePosition;

    public bool keyRepeat { get; set; }

    public string clipboard {
        get => renderer.window?.ClipboardString ?? "";
        set {
            if(renderer.window is null)
                return;
            renderer.window.ClipboardString = value;
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

    public void Setup() {
        if(renderer.window is null)
            return;
        renderer.window.KeyDown += OnKeyDown;
        renderer.window.TextInput += OnTextInput;
        renderer.window.MouseMove += OnMouseMove;
        renderer.window.MouseWheel += OnMouseWheel;
    }

    public void Finish() {
        if(renderer.window is null)
            return;
        renderer.window.KeyDown -= OnKeyDown;
        renderer.window.TextInput -= OnTextInput;
        renderer.window.MouseMove -= OnMouseMove;
        renderer.window.MouseWheel -= OnMouseWheel;
    }

    private void OnKeyDown(KeyboardKeyEventArgs key) {
        if(key.IsRepeat && !keyRepeat)
            return;
        keyDown?.Invoke(new IInput.KeyDownArgs(Converters.ToPerKey(key.Key), key.Command, key.Shift, key.Control,
            key.Alt));
    }
    private void OnTextInput(TextInputEventArgs text) => EnterText(text.AsString);
    private void OnMouseMove(MouseMoveEventArgs mouse) => UpdateMousePosition(mouse.X, mouse.Y);
    private void OnMouseWheel(MouseWheelEventArgs scroll) => ScrollMouse(scroll.Offset.Y);

    public void Update(TimeSpan time) {
        _previousMousePosition = _mousePosition;
        _previousAccurateMousePosition = _accurateMousePosition;
        _previousNormalizedMousePosition = _normalizedMousePosition;
    }

    public bool KeyPressed(KeyCode key) {
        if(block || renderer.window is null)
            return false;
        Keys otkKey = Converters.ToOtkKey(key);
        return otkKey != Keys.Unknown && renderer.window.IsKeyDown(otkKey);
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

    public bool MouseButtonPressed(MouseButton button) => !block && renderer.window is not null &&
        renderer.window.IsMouseButtonDown(Converters.ToOtkMouseButton(button));

    private void EnterText(string text) => textEntered?.Invoke(new IInput.TextEnteredArgs(text));

    private void UpdateMousePosition(float mouseX, float mouseY) {
        if(!renderer.focused) {
            _mousePosition = new Vector2Int(-1, -1);
            _accurateMousePosition = new Vector2(-1f, -1f);
            _normalizedMousePosition = new Vector2(-1f, -1f);
            return;
        }

        Vector2 pixelMousePosition = new(
            mouseX - renderer.window?.ClientSize.X * 0.5f + renderer.size.x * renderer.font.size.x * 0.5f ?? 0f,
            mouseY - renderer.window?.ClientSize.Y * 0.5f + renderer.size.y * renderer.font.size.y * 0.5f ?? 0f);
        _accurateMousePosition = new Vector2(
            pixelMousePosition.X / renderer.font.size.x,
            pixelMousePosition.Y / renderer.font.size.y);
        _mousePosition = new Vector2Int((int)_accurateMousePosition.X, (int)_accurateMousePosition.Y);
        _normalizedMousePosition =
            new Vector2(pixelMousePosition.X / (renderer.size.x * renderer.font.size.x - 1),
                pixelMousePosition.Y / (renderer.size.y * renderer.font.size.y - 1));
    }

    private void ScrollMouse(float delta) => scrolled?.Invoke(new IInput.ScrolledArgs(delta));
}
