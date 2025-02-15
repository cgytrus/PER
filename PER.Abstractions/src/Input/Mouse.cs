using System;
using System.Numerics;
using PER.Util;

namespace PER.Abstractions.Input;

public interface IMouse : IDevice {
    public readonly struct Position(Vector2Int snapped, Vector2 accurate) {
        public Vector2Int snapped { get; } = snapped;
        public Vector2 accurate { get; } = accurate;
        public static implicit operator Vector2Int(Position x) => x.snapped;
        public static implicit operator Vector2(Position x) => x.accurate;
    }

    public readonly struct Positions(Position current, Position prev) {
        public Vector2Int snapped => _current;
        public Vector2 accurate => _current;
        public Position prev { get; } = prev;

        private readonly Position _current = current;
        public static implicit operator Vector2Int(Positions x) => x._current;
        public static implicit operator Vector2(Positions x) => x._current;

        public Positions(Vector2Int snapped, Vector2 accurate) :
            this(new Position(snapped, accurate), new Position(snapped, accurate)) { }
        public Positions() : this(new Vector2Int(-1, -1), new Vector2(-1f, -1f)) { }
    }

    public InputReq<Positions> GetPosition();

    public InputReq<(bool, Positions)> GetButton(MouseButton button);
    public InputReq<(bool, Positions)> GetButton(MouseButton button, Bounds area);

    public InputReq<(float, Positions)> GetScroll();
    public InputReq<(float, Positions)> GetScroll(Bounds area);

    public InputReq<(bool, Positions)> GetIsWithin(Bounds area);
    public InputReq<(TimeSpan?, Positions)> GetWasWithin(Bounds area);
}

public abstract class Mouse<TSelf> : Device<TSelf>, IMouse where TSelf : Mouse<TSelf> {
    protected abstract IMouse.Position position { get; }
    protected abstract IMouse.Position prevPosition { get; }
    private IMouse.Positions positions => new(position, prevPosition);

    private TimeSpan _time;
    private TimeSpan _prevTime;

    protected Mouse() {
        _positions = new InputRequests<IMouse.Positions>(() => positions, new IMouse.Positions());
        _buttons = new InputRequests<MouseButton, (bool, IMouse.Positions)>(button => (ProcButton(button), positions),
            (false, new IMouse.Positions()));
        _scrolls = new InputRequests<(float, IMouse.Positions)>(() => (ProcScroll(), positions), (0f, new IMouse.Positions()));
        _isWithins = new InputRequests<(bool, IMouse.Positions)>(() => (true, positions), (false, new IMouse.Positions()));
        _wasWithins = new InputRequests<float, (TimeSpan?, IMouse.Positions)>(d =>
            (new TimeSpan((long)Meth.Lerp(_prevTime.Ticks, _time.Ticks, d)), positions), (null, new IMouse.Positions()));
    }

    private readonly InputRequests<IMouse.Positions> _positions;
    public InputReq<IMouse.Positions> GetPosition() => _positions.Make();

    private readonly InputRequests<MouseButton, (bool, IMouse.Positions)> _buttons;
    public InputReq<(bool, IMouse.Positions)> GetButton(MouseButton button) => _buttons.Make(button);
    public InputReq<(bool, IMouse.Positions)> GetButton(MouseButton button, Bounds area) => area.Contains(position) ? GetButton(button) : _buttons.MakeDefault();
    protected abstract bool ProcButton(MouseButton button);

    private readonly InputRequests<(float, IMouse.Positions)> _scrolls;
    public InputReq<(float, IMouse.Positions)> GetScroll() => _scrolls.Make();
    public InputReq<(float, IMouse.Positions)> GetScroll(Bounds area) => area.Contains(position) ? GetScroll() : _scrolls.MakeDefault();
    protected abstract float ProcScroll();

    private readonly InputRequests<(bool, IMouse.Positions)> _isWithins;
    public InputReq<(bool, IMouse.Positions)> GetIsWithin(Bounds area) => area.Contains(position) ? _isWithins.Make() : _isWithins.MakeDefault();

    private readonly InputRequests<float, (TimeSpan?, IMouse.Positions)> _wasWithins;
    public InputReq<(TimeSpan?, IMouse.Positions)> GetWasWithin(Bounds area) =>
        Intersect.RectSegmentOut(area, prevPosition, position, out float d) ? _wasWithins.Make(d) :
            _wasWithins.MakeDefault();

    public override void Update(TimeSpan time) {
        _buttons.Clear();
        _positions.Clear();
        _scrolls.Clear();
        _isWithins.Clear();
        _wasWithins.Clear();

        _prevTime = _time;
        _time = time;
    }
}
