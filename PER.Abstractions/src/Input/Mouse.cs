using System;
using System.Numerics;
using PER.Util;

namespace PER.Abstractions.Input;

public abstract class Mouse : Device {
    public readonly struct Position(Vector2Int snapped, Vector2 accurate) {
        public Vector2Int snapped { get; } = snapped;
        public Vector2 accurate { get; } = accurate;
        public static implicit operator Vector2Int(Position x) => x.snapped;
        public static implicit operator Vector2(Position x) => x.accurate;
    }

    protected abstract Position position { get; }
    protected abstract Position prevPosition { get; }
    private Positions positions => new(position, prevPosition);

    private TimeSpan _time;
    private TimeSpan _prevTime;

    public readonly struct Positions(Position current, Position prev) {
        public Vector2Int snapped => _current;
        public Vector2 accurate => _current;
        public Position prev { get; } = prev;

        private readonly Position _current = current;
        public static implicit operator Vector2Int(Positions x) => x._current;
        public static implicit operator Vector2(Positions x) => x._current;

        public Positions(Vector2Int snapped, Vector2 accurate) :
            this(new Position(snapped, accurate), new Position(snapped, accurate)) { }
        public Positions() :
            this(new Vector2Int(-1, -1), new Vector2(-1f, -1f)) { }
    }

    protected Mouse() {
        _positions = new InputRequests<Positions>(() => positions, new Positions());
        _buttons = new InputRequests<MouseButton, (bool, Positions)>(button => (ProcButton(button), positions),
            (false, new Positions()));
        _scrolls = new InputRequests<(float, Positions)>(() => (ProcScroll(), positions), (0f, new Positions()));
        _isWithins = new InputRequests<(bool, Positions)>(() => (true, positions), (false, new Positions()));
        _wasWithins = new InputRequests<float, (TimeSpan?, Positions)>(d =>
            (new TimeSpan((long)Meth.Lerp(_prevTime.Ticks, _time.Ticks, d)), positions), (null, new Positions()));
    }

    private readonly InputRequests<Positions> _positions;
    public InputReq<Positions> GetPosition() => _positions.Make();

    private readonly InputRequests<MouseButton, (bool, Positions)> _buttons;
    public InputReq<(bool, Positions)> GetButton(MouseButton button) => _buttons.Make(button);
    public InputReq<(bool, Positions)> GetButton(MouseButton button, Bounds area) => area.Contains(position) ? GetButton(button) : _buttons.MakeDefault();
    protected abstract bool ProcButton(MouseButton button);

    private readonly InputRequests<(float, Positions)> _scrolls;
    public InputReq<(float, Positions)> GetScroll() => _scrolls.Make();
    public InputReq<(float, Positions)> GetScroll(Bounds area) => area.Contains(position) ? GetScroll() : _scrolls.MakeDefault();
    protected abstract float ProcScroll();

    private readonly InputRequests<(bool, Positions)> _isWithins;
    public InputReq<(bool, Positions)> GetIsWithin(Bounds area) => area.Contains(position) ? _isWithins.Make() : _isWithins.MakeDefault();

    private readonly InputRequests<float, (TimeSpan?, Positions)> _wasWithins;
    public InputReq<(TimeSpan?, Positions)> GetWasWithin(Bounds area) =>
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
