using System;
using System.Collections.Generic;

namespace PER.Abstractions.Input;

public interface IKeyboard : IDevice {
    public InputReq<bool> GetKey(KeyCode key);
    public InputReq<int> GetKeyDown(ModifierKey modifier, KeyCode key, bool repeat);
    public InputReq<int> GetKeyDown(KeyCode key, bool repeat);
    public InputReq<IEnumerable<string>> GetText();
}
public abstract class Keyboard<TSelf> : Device<TSelf>, IKeyboard where TSelf : Keyboard<TSelf> {
    protected Keyboard() {
        _keys = new InputRequests<KeyCode, bool>(ProcKey, false);
        _keyDowns = new InputRequests<(KeyCode key, ModifierKey modifier, bool repeat), int>(ProcKeyDown, 0);
        _texts = new InputRequests<IEnumerable<string>>(ProcText, Array.Empty<string>());
    }

    private readonly InputRequests<KeyCode, bool> _keys;
    public InputReq<bool> GetKey(KeyCode key) => _keys.Make(key);
    protected abstract bool ProcKey(KeyCode key);

    private readonly InputRequests<(KeyCode key, ModifierKey modifier, bool repeat), int> _keyDowns;
    public InputReq<int> GetKeyDown(ModifierKey modifier, KeyCode key, bool repeat) =>
        _keyDowns.Make((key, modifier, repeat));
    public InputReq<int> GetKeyDown(KeyCode key, bool repeat) => GetKeyDown(ModifierKey.None, key, repeat);
    protected abstract int ProcKeyDown((KeyCode key, ModifierKey modifier, bool repeat) data);

    private readonly InputRequests<IEnumerable<string>> _texts;
    public InputReq<IEnumerable<string>> GetText() => _texts.Make();
    protected abstract IEnumerable<string> ProcText();

    public override void Update(TimeSpan time) {
        _keys.Clear();
        _keyDowns.Clear();
        _texts.Clear();
    }
}
