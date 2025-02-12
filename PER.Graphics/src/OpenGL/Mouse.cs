using System;
using System.Collections.Generic;
using System.Numerics;
using CommunityToolkit.HighPerformance;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using PER.Abstractions;
using PER.Abstractions.Input;
using PER.Util;
using MouseButton = PER.Abstractions.Input.MouseButton;

namespace PER.Graphics.OpenGL;

public class Mouse(Renderer renderer) : PER.Abstractions.Input.Mouse, ISetupable {
    private Vector2Int _mousePosition = new(-1, -1);
    private Vector2 _accurateMousePosition = new(-1f, -1f);
    private Vector2Int _previousMousePosition = new(-1, -1);
    private Vector2 _previousAccurateMousePosition = new(-1f, -1f);

    private float _scroll;

    protected override Position position => new(_mousePosition, _accurateMousePosition);
    protected override Position prevPosition => new(_previousMousePosition, _previousAccurateMousePosition);

    protected override bool ProcButton(MouseButton button) => renderer.window is not null &&
        renderer.window.IsMouseButtonDown(Converters.ToOtkMouseButton(button));

    protected override float ProcScroll() => _scroll;

    public void Setup() {
        if (renderer.window is null)
            return;
        renderer.window.MouseMove += OnMouseMove;
        renderer.window.MouseWheel += OnMouseWheel;
    }

    public override void Finish() {
        if (renderer.window is null)
            return;
        renderer.window.MouseMove -= OnMouseMove;
        renderer.window.MouseWheel -= OnMouseWheel;
    }

    private readonly List<MouseMoveEventArgs> _mouseMoveEvents = [];
    private readonly List<MouseWheelEventArgs> _mouseWheelEvents = [];

    private void OnMouseMove(MouseMoveEventArgs e) => _mouseMoveEvents.Add(e);
    private void OnMouseWheel(MouseWheelEventArgs e) => _mouseWheelEvents.Add(e);

    public override void Update(TimeSpan time) {
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
                mouse.X - renderer.window?.ClientSize.X * 0.5f + renderer.size.x * renderer.font.size.x * 0.5f ?? 0f,
                mouse.Y - renderer.window?.ClientSize.Y * 0.5f + renderer.size.y * renderer.font.size.y * 0.5f ?? 0f);
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
