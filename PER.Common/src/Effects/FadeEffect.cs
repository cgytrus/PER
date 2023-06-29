﻿using System.Numerics;

using JetBrains.Annotations;

using PER.Abstractions.Rendering;
using PER.Util;

namespace PER.Common.Effects;

[PublicAPI]
public class FadeEffect : IEffect {
    private enum State { None, Out, In }

    public IEnumerable<PipelineStep>? pipeline => null;
    public bool hasModifiers => true;
    public bool drawable => false;

    public bool fading => _state != State.None;
    private float t => (float)_stopwatch.time.TotalSeconds / _state switch {
        State.Out => _outTime,
        State.In => _inTime,
        _ => 0f
    };

    private float _lastT;
    private bool _callbackThisFrame; // keep screen black for a frame after we called the callback

    private State _state;
    private float _outTime;
    private float _inTime;
    private Action? _callback;
    private readonly Stopwatch _stopwatch = new();
    private readonly Dictionary<Vector2Int, float> _speeds = new(128);

    private const float MinSpeed = 3f;
    private const float MaxSpeed = 5f;

    public void Start(float outTime, float inTime, Action middleCallback) {
        _outTime = outTime;
        _inTime = inTime;
        _callback = middleCallback;
        _state = State.Out;
        _speeds.Clear();
        _stopwatch.Reset();
    }

    public void ApplyModifiers(Vector2Int at, ref Vector2 position, ref RenderCharacter character) {
        if(!_speeds.ContainsKey(at))
            _speeds.Add(at, Random.Shared.NextSingle(MinSpeed, MaxSpeed));
        float t = _lastT * _speeds[at];
        if(_callbackThisFrame)
            t = 0f;
        else if(_state == State.Out)
            t = 1f - t;
        character = character with {
            background = new Color(character.background.r, character.background.g, character.background.b,
                MoreMath.Lerp(0f, character.background.a, t)),
            foreground = new Color(character.foreground.r, character.foreground.g, character.foreground.b,
                MoreMath.Lerp(0f, character.foreground.a, t))
        };
    }

    public void Update(bool fullscreen) {
        if(_callbackThisFrame)
            _callbackThisFrame = false;
        _lastT = t;
        if(_lastT < 1f) return;
        switch(_state) {
            case State.Out:
                _callback?.Invoke();
                _callbackThisFrame = true;
                _state = State.In;
                _speeds.Clear();
                _stopwatch.Reset();
                break;
            case State.In:
                _state = State.None;
                break;
        }
    }

    public void Draw(Vector2Int position) { }
}
