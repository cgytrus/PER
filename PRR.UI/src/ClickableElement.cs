﻿using JetBrains.Annotations;

using PER.Abstractions.Audio;
using PER.Abstractions.Input;
using PER.Abstractions.Rendering;
using PER.Util;

namespace PRR.UI;

[PublicAPI]
public abstract class ClickableElement : Element {
    public enum State { None, Inactive, Idle, FakeHovered, Hovered, FakeClicked, Clicked, Hotkey }
    public static IPlayable? clickSound { get; set; }

    protected abstract string type { get; }

    public override Vector2Int size {
        get => base.size;
        set {
            base.size = value;
            _animSpeeds = new float[value.y, value.x];
        }
    }

    public virtual bool active { get; set; } = true;

    public Color inactiveColor { get; set; } = new(0.1f, 0.1f, 0.1f);
    public Color idleColor { get; set; } = Color.black;
    public Color hoverColor { get; set; } = Color.white;
    public Color clickColor { get; set; } = new(0.4f, 0.4f, 0.4f);

    public event EventHandler? onClick;
    public event EventHandler? onHover;
    public event EventHandler? onPush;
    public event EventHandler? onRelease;

    public State currentState { get; private set; } = State.None;
    public IMouse.Positions mousePosition { get; private set; }

    protected bool toggledSelf {
        get => _toggled;
        set {
            if(_toggled == value)
                return;
            _toggled = value;
            _toggledChanged = true;
        }
    }

    private InputReq<bool>? _hotkeyPressed;
    protected abstract InputReq<bool>? hotkeyPressed { get; }

    protected const float MinSpeed = 3f;
    protected const float MaxSpeed = 5f;

    private bool _clickLocked;
    private InputReq<(bool, IMouse.Positions)> _mouseClicked;
    private InputReq<(bool, IMouse.Positions)> _mouseOver;
    private InputReq<(TimeSpan?, IMouse.Positions)> _mouseWasOver;

    private float[,] _animSpeeds = new float[0, 0];
    private TimeSpan _animStartTime;
    private Color _animBackgroundColorStart;
    private Color _animBackgroundColorEnd;
    private Color _animForegroundColorStart;
    private Color _animForegroundColorEnd;
    private bool _toggled;
    private bool _toggledChanged;
    private TimeSpan _lastTime;

    public override void Input() {
        _hotkeyPressed = hotkeyPressed;
        IMouse mouse = input.Get<IMouse>();
        _mouseClicked = _clickLocked ? mouse.GetButton(MouseButton.Left) : mouse.GetButton(MouseButton.Left, bounds);
        _mouseOver = mouse.GetIsWithin(bounds);
        _mouseWasOver = mouse.GetWasWithin(bounds);
    }

    protected virtual void UpdateState(TimeSpan time) {
        State prevState = currentState;

        (_clickLocked, mousePosition) = _mouseClicked.Read();

        bool mouseOver = _mouseOver.Read().Item1 || _clickLocked;
        TimeSpan? mouseWasOver = _mouseWasOver.Read().Item1 ??
            (_clickLocked && mouseOver ? time : null);

        State clickedState = mouseOver ? State.Clicked : State.FakeClicked;
        State hoveredState = mouseOver ? State.Hovered : State.FakeHovered;
        State overState = _clickLocked ? clickedState : hoveredState;

        currentState = active ? _hotkeyPressed ?? false ? State.Hotkey :
            mouseWasOver is not null ? overState : State.Idle : State.Inactive;

        if (currentState != prevState || _toggledChanged)
            StateChanged(currentState == overState ? mouseWasOver!.Value : time, prevState, currentState);

        _toggledChanged = false;
        _lastTime = time;
    }

    private void StateChanged(TimeSpan time, State from, State to) {
        bool instant = from == State.None;

        ExecuteStateChangeActions(from, to);

        switch(to) {
            case State.Inactive:
                StartAnimation(time, _toggled ? inactiveColor : idleColor,
                    _toggled ? idleColor : inactiveColor, instant);
                break;
            case State.Idle:
                StartAnimation(time, _toggled ? clickColor : idleColor, _toggled ? idleColor : hoverColor, instant);
                break;
            case State.FakeHovered:
            case State.Hovered:
                StartAnimation(time, hoverColor, idleColor, instant);
                break;
            case State.FakeClicked:
            case State.Clicked:
                StartAnimation(time, clickColor, idleColor, instant);
                break;
        }
    }

    private void ExecuteStateChangeActions(State from, State to) {
        switch(to) {
            case State.Idle when from is State.Hotkey:
            case State.Hovered when from is State.Clicked or State.Hotkey:
                Click();
                break;
            case State.Hovered:
                onHover?.Invoke(this, EventArgs.Empty);
                break;
            case State.Clicked when from is not State.Clicked and not State.Hotkey:
            case State.Hotkey when from is not State.Clicked and not State.Hotkey:
                onPush?.Invoke(this, EventArgs.Empty);
                break;
        }

        if(to is not State.Clicked and not State.Hotkey && from is State.Clicked or State.Hotkey)
            onRelease?.Invoke(this, EventArgs.Empty);
    }

    protected virtual void Click() {
        clickSound?.Play();
        onClick?.Invoke(this, EventArgs.Empty);
    }

    private void StartAnimation(TimeSpan time, Color background, Color foreground, bool instant) {
        for(int y = 0; y < size.y; y++)
            for(int x = 0; x < size.x; x++)
                _animSpeeds[y, x] = Random.Shared.NextSingle(MinSpeed, MaxSpeed);
        _animStartTime = time;
        _animBackgroundColorStart = instant ? background : _animBackgroundColorEnd;
        _animBackgroundColorEnd = background;
        _animForegroundColorStart = instant ? foreground : _animForegroundColorEnd;
        _animForegroundColorEnd = foreground;
    }

    public override void Update(TimeSpan time) {
        if (!enabled) {
            currentState = State.None;
            return;
        }

        // clear before drawing
        for(int y = 0; y < size.y; y++)
            for(int x = 0; x < size.x; x++)
                renderer.DrawCharacter(position + new Vector2Int(x, y),
                    new RenderCharacter('\0', renderer.background, Color.transparent));

        UpdateState(time);
        CustomUpdate(time);

        float animTime = (float)(time - _animStartTime).TotalSeconds;
        for(int y = 0; y < size.y; y++) {
            for(int x = 0; x < size.x; x++) {
                float t = animTime * _animSpeeds[y, x];
                Color backgroundColor = Color.LerpColors(_animBackgroundColorStart, _animBackgroundColorEnd, t);
                Color foregroundColor = Color.LerpColors(_animForegroundColorStart, _animForegroundColorEnd, t);

                DrawCharacter(x, y, backgroundColor, foregroundColor);
            }
        }
    }

    protected abstract void DrawCharacter(int x, int y, Color backgroundColor, Color foregroundColor);

    protected abstract void CustomUpdate(TimeSpan time);

    public override void UpdateColors(Dictionary<string, Color> colors, List<string> layoutNames, string id,
        string? special) {
        if(TryGetColor(colors, type, layoutNames, id, "inactive", special, out Color color) ||
            TryGetColor(colors, "clickable", layoutNames, id, "inactive", special, out color))
            inactiveColor = color;
        if(TryGetColor(colors, type, layoutNames, id, "idle", special, out color) ||
            TryGetColor(colors, "clickable", layoutNames, id, "idle", special, out color))
            idleColor = color;
        if(TryGetColor(colors, type, layoutNames, id, "hover", special, out color) ||
            TryGetColor(colors, "clickable", layoutNames, id, "hover", special, out color))
            hoverColor = color;
        if(TryGetColor(colors, type, layoutNames, id, "click", special, out color) ||
            TryGetColor(colors, "clickable", layoutNames, id, "click", special, out color))
            clickColor = color;
        // if UpdateColors is called from some update function,
        // _lastTime will be time at the current frame (or at least the last frame)
        // otherwise, it doesn't really matter
        StateChanged(_lastTime, currentState, currentState);
    }
}
