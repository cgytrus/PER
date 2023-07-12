using JetBrains.Annotations;

using PER.Abstractions;
using PER.Abstractions.Rendering;
using PER.Util;

namespace PER.Common.Effects;

[PublicAPI]
public class FadeEffect : IModifierEffect, IUpdatable {
    private enum State { None, Starting, Out, In }

    public bool fading => _state != State.None;

    private TimeSpan _startTime;

    private float _lastT;
    private bool _callbackThisFrame; // keep screen black for a frame after we called the callback

    private State _state;
    private float _outTime;
    private float _inTime;
    private Action? _callback;

    private const float MinSpeed = 3f;
    private const float MaxSpeed = 5f;

    public void Start(float outTime, float inTime, Action middleCallback) {
        _outTime = outTime;
        _inTime = inTime;
        _callback = middleCallback;
        _state = State.Starting;
    }

    public void ApplyModifiers(Vector2Int at, ref Vector2Int offset, ref RenderCharacter character) {
        float rand;
        unchecked {
            // idk what im doing xd
            int rand1 = (at.x + 691337420) ^ (at.y + 133742069) ^ ((int)_startTime.Ticks + 123456789) ^
                at.GetHashCode();
            ushort rand2 = (ushort)rand1;
            rand = rand2 / (float)ushort.MaxValue;
        }
        float t = _lastT * (rand * (MaxSpeed - MinSpeed) + MinSpeed);
        if(_state == State.Out)
            t = 1f - t;
        character = character with {
            background = new Color(character.background.r, character.background.g, character.background.b,
                MoreMath.Lerp(0f, character.background.a, t)),
            foreground = new Color(character.foreground.r, character.foreground.g, character.foreground.b,
                MoreMath.Lerp(0f, character.foreground.a, t))
        };
    }

    public void Update(TimeSpan time) {
        switch(_state) {
            case State.None: return;
            case State.Starting:
                _state = State.Out;
                _startTime = time;
                break;
        }
        if(_callbackThisFrame) {
            _startTime = time;
            _callbackThisFrame = false;
        }
        _lastT = (float)(time - _startTime).TotalSeconds / _state switch {
            State.Out => _outTime,
            State.In => _inTime,
            _ => 0f
        };
        if(_lastT < 1f)
            return;
        _lastT = 0f;
        switch(_state) {
            case State.Out:
                _callback?.Invoke();
                _callbackThisFrame = true;
                _state = State.In;
                break;
            case State.In:
                _state = State.None;
                break;
        }
    }
}
