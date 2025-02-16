using System;
using System.Collections.Generic;
using System.Numerics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using PER.Abstractions.Input;
using PER.Abstractions.Meta;
using PER.Graphics.OpenGL;
using PER.Util;
using MouseButton = PER.Abstractions.Input.MouseButton;

namespace PER.Input.Glfw;

public class Mouse : Mouse<Mouse> {
    private static NativeWindow? window => (renderer as Renderer)?.window;

    private Vector2Int _mousePosition = new(-1, -1);
    private Vector2 _accurateMousePosition = new(-1f, -1f);
    private Vector2Int _previousMousePosition = new(-1, -1);
    private Vector2 _previousAccurateMousePosition = new(-1f, -1f);

    private float _scroll;

    protected override IMouse.Position position => new(_mousePosition, _accurateMousePosition);
    protected override IMouse.Position prevPosition => new(_previousMousePosition, _previousAccurateMousePosition);

    protected override bool ProcButton(MouseButton button) => window is not null &&
        window.IsMouseButtonDown(Converters.ToOtkMouseButton(button));

    protected override float ProcScroll() => _scroll;

    public override void Setup() {
        if (window is null)
            return;
        window.MouseMove += OnMouseMove;
        window.MouseWheel += OnMouseWheel;
    }

    public override void Finish() {
        if (window is null)
            return;
        window.MouseMove -= OnMouseMove;
        window.MouseWheel -= OnMouseWheel;
    }

    private readonly List<MouseMoveEventArgs> _mouseMoveEvents = [];
    private readonly List<MouseWheelEventArgs> _mouseWheelEvents = [];

    private void OnMouseMove(MouseMoveEventArgs e) => _mouseMoveEvents.Add(e);
    private void OnMouseWheel(MouseWheelEventArgs e) => _mouseWheelEvents.Add(e);

    [RequiresHead]
    public override void Update(TimeSpan time) {
        RequireHead();
        base.Update(time);

        _previousMousePosition = _mousePosition;
        _previousAccurateMousePosition = _accurateMousePosition;

        foreach (MouseMoveEventArgs mouse in _mouseMoveEvents) {
            if (!renderer.focused) {
                _mousePosition = new Vector2Int(-1, -1);
                _accurateMousePosition = new Vector2(-1f, -1f);
                continue;
            }

            Vector2 pixelMousePosition = new(
                mouse.X - window?.ClientSize.X * 0.5f + renderer.size.x * renderer.font.size.x * 0.5f ?? 0f,
                mouse.Y - window?.ClientSize.Y * 0.5f + renderer.size.y * renderer.font.size.y * 0.5f ?? 0f);
            _accurateMousePosition = new Vector2(
                pixelMousePosition.X / renderer.font.size.x,
                pixelMousePosition.Y / renderer.font.size.y);
            _mousePosition = new Vector2Int((int)_accurateMousePosition.X, (int)_accurateMousePosition.Y);
        }

        foreach (MouseWheelEventArgs scroll in _mouseWheelEvents) {
            _scroll += scroll.Offset.Y;
        }

        _mouseMoveEvents.Clear();
        _mouseWheelEvents.Clear();
    }
}
