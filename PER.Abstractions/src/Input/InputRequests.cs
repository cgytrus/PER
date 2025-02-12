using System;
using System.Collections.Generic;

namespace PER.Abstractions.Input;

public readonly struct InputReq<TData> {
    public static implicit operator TData(InputReq<TData> x) => x.Read();
    public TData Read() => _coll.Get(_handle);

    private readonly IInputRequestCollection<TData> _coll;
    private readonly uint _handle;

    internal InputReq(IInputRequestCollection<TData> coll, uint handle) {
        _coll = coll;
        _handle = handle;
    }
}

internal interface IInputRequestCollection<out TData> {
    public TData Get(uint handle);
}

public class InputRequests<TInput, TOutput>(Func<TInput, TOutput> getValid, TOutput defaultOut) :
    IInputRequestCollection<TOutput> where TInput : notnull {
    private readonly Dictionary<TInput, uint> _handles = [];
    private readonly Dictionary<uint, TInput> _requests = [];
    private readonly HashSet<TInput> _readInputs = [];
    private uint _handle;

    public void Clear() {
        _handles.Clear();
        _requests.Clear();
        _readInputs.Clear();
        _handle = 0;
    }

    public InputReq<TOutput> MakeDefault() => new(this, 0);

    public InputReq<TOutput> Make(TInput data) {
        if (_readInputs.Contains(data))
            throw new InvalidOperationException("Cannot request new input after one of the same was already read.");
        _handle++;
        _handles[data] = _handle;
        _requests[_handle] = data;
        return new InputReq<TOutput>(this, _handle);
    }

    public TOutput Get(uint handle) {
        if (handle == 0)
            return defaultOut;
        if (!_requests.Remove(handle, out TInput? input))
            throw new InvalidOperationException("Tried to read already read input request.");
        _readInputs.Add(input);
        return _handles[input] == handle ? getValid(input) : defaultOut;
    }
}

public class InputRequests<TOutput>(Func<TOutput> getValid, TOutput defaultOut) :
    IInputRequestCollection<TOutput> {
    private readonly HashSet<uint> _requests = [];
    private bool _readInput;
    private uint _handle;

    public void Clear() {
        _requests.Clear();
        _readInput = false;
        _handle = 0;
    }

    public InputReq<TOutput> MakeDefault() => new(this, 0);

    public InputReq<TOutput> Make() {
        if (_readInput)
            throw new InvalidOperationException("Cannot request new input after one of the same was already read.");
        _handle++;
        _requests.Add(_handle);
        return new InputReq<TOutput>(this, _handle);
    }

    public TOutput Get(uint handle) {
        if (handle == 0)
            return defaultOut;
        if (!_requests.Remove(handle))
            throw new InvalidOperationException("Tried to read already read input request.");
        _readInput = true;
        return _handle == handle ? getValid() : defaultOut;
    }
}
