using System.Numerics;

using JetBrains.Annotations;

using PER.Abstractions.Rendering;
using PER.Util;

namespace PER.Common.Effects;

[PublicAPI]
public class FadeEffect : IModifierEffect, IUpdatableEffect {
    private enum State { None, Out, In }

    public bool fading => _state != State.None;
    private float t => (float)_stopwatch.time.TotalSeconds / _state switch {
        State.Out => _outTime,
        State.In => _inTime,
        _ => 0f
    };

    private static readonly Stopwatch globalTimer = new();
    private int _startTicks;

    private float _lastT;
    private bool _callbackThisFrame; // keep screen black for a frame after we called the callback

    private State _state;
    private float _outTime;
    private float _inTime;
    private Action? _callback;
    private readonly Stopwatch _stopwatch = new();

    private const float MinSpeed = 3f;
    private const float MaxSpeed = 5f;

    public void Start(float outTime, float inTime, Action middleCallback) {
        _outTime = outTime;
        _inTime = inTime;
        _callback = middleCallback;
        _state = State.Out;
        _stopwatch.Reset();
        _startTicks = (int)globalTimer.ticks;
    }

    public void ApplyModifiers(Vector2Int at, ref Vector2 position, ref RenderCharacter character) {
        float rand;
        unchecked {
            // idk what im doing xd
            int rand1 = (at.x + 691337420) ^ (at.y + 133742069) ^ (_startTicks + 123456789) ^ at.GetHashCode();
            ushort rand2 = (ushort)rand1;
            rand = rand2 / (float)ushort.MaxValue;
        }
        float t = _lastT * (rand * (MaxSpeed - MinSpeed) + MinSpeed);
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

    public void Update() {
        if(_callbackThisFrame)
            _callbackThisFrame = false;
        _lastT = t;
        if(_lastT < 1f) return;
        switch(_state) {
            case State.Out:
                _callback?.Invoke();
                _callbackThisFrame = true;
                _state = State.In;
                _stopwatch.Reset();
                _startTicks = (int)globalTimer.ticks;
                break;
            case State.In:
                _state = State.None;
                break;
        }
    }
}
