using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

using PER.Util;

namespace PER.Abstractions.Environment;

public abstract class Chunk<TLevel, TChunk, TObject> : IUpdatable, ITickable
    where TLevel : Level<TLevel, TChunk, TObject>
    where TChunk : Chunk<TLevel, TChunk, TObject>, new()
    where TObject : LevelObject<TLevel, TChunk, TObject> {
    internal int ticks { get; set; }

    private readonly List<TObject?> _objects = new();
    private readonly List<IUpdatable?> _updatables = new();
    private readonly List<ITickable?> _tickables = new();

    public void Add(TObject obj) {
        _objects.Add(obj);
        _objects.Sort((a, b) => (a?.layer ?? int.MinValue).CompareTo(b?.layer ?? int.MinValue));
        if(obj is IUpdatable updatable)
            _updatables.Add(updatable);
        if(obj is ITickable tickable)
            _tickables.Add(tickable);
    }

    public void Remove(TObject obj) {
        int index = _objects.IndexOf(obj);
        if(index >= 0)
            _objects[index] = null;
        if(obj is IUpdatable updatable) {
            index = _updatables.IndexOf(updatable);
            if(index >= 0)
                _updatables[index] = null;
        }
        // ReSharper disable once InvertIf
        if(obj is ITickable tickable) {
            index = _tickables.IndexOf(tickable);
            if(index >= 0)
                _tickables[index] = null;
        }
    }

    public void Update(TimeSpan time) {
        // ReSharper disable once ForCanBeConvertedToForeach
        for(int i = 0; i < _updatables.Count; i++) {
            IUpdatable? updatable = _updatables[i];
            if(updatable is TObject { inLevelInt: true })
                updatable.Update(time);
        }
        ProcessRemoved();
    }

    public void Draw() {
        // ReSharper disable once ForCanBeConvertedToForeach
        for(int i = 0; i < _objects.Count; i++) {
            TObject? obj = _objects[i];
            if(obj is not null && obj.inLevelInt)
                obj.Draw();
        }
    }

    public void Tick(TimeSpan time) {
        // ReSharper disable once ForCanBeConvertedToForeach
        for(int i = 0; i < _tickables.Count; i++) {
            ITickable? tickable = _tickables[i];
            if(tickable is TObject { inLevelInt: true })
                tickable.Tick(time);
        }
        ProcessRemoved();
    }

    private void ProcessRemoved() {
        _objects.RemoveAll(x => x is null || !x.inLevelInt);
        _updatables.RemoveAll(x => x is null or TObject { inLevelInt: false });
        _tickables.RemoveAll(x => x is null or TObject { inLevelInt: false });
    }

    public void PopulateDirty(List<TObject> dirtyObjects) {
        // ReSharper disable once ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
        foreach(TObject? obj in _objects)
            if(obj is not null && obj.inLevelInt && (obj.positionDirty || obj.dirty))
                dirtyObjects.Add(obj);
    }

    public bool HasObjectAt(Vector2Int position) {
        // ReSharper disable once ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
        foreach(TObject? obj in _objects)
            if(obj is not null && obj.inLevelInt && obj.position == position)
                return true;
        return false;
    }
    public bool HasObjectAt(Vector2Int position, int minLayer) {
        // ReSharper disable once ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
        foreach(TObject? obj in _objects)
            if(obj is not null && obj.inLevelInt && obj.position == position && obj.layer >= minLayer)
                return true;
        return false;
    }

    public bool HasObjectAt(Vector2Int position, Type type) {
        // ReSharper disable once ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
        foreach(TObject? obj in _objects)
            if(obj is not null && obj.inLevelInt && obj.GetType() == type && obj.position == position)
                return true;
        return false;
    }
    public bool HasObjectAt(Vector2Int position, int minLayer, Type type) {
        // ReSharper disable once ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
        foreach(TObject? obj in _objects)
            if(obj is not null && obj.inLevelInt && obj.GetType() == type && obj.position == position &&
                obj.layer >= minLayer)
                return true;
        return false;
    }

    public bool HasObjectAt<T>(Vector2Int position) where T : class {
        // ReSharper disable once ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
        foreach(TObject? obj in _objects)
            if(obj is T && obj.inLevelInt && obj.position == position)
                return true;
        return false;
    }
    public bool HasObjectAt<T>(Vector2Int position, int minLayer) where T : class {
        // ReSharper disable once ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
        foreach(TObject? obj in _objects)
            if(obj is T && obj.inLevelInt && obj.position == position && obj.layer >= minLayer)
                return true;
        return false;
    }

    public bool TryGetObjectAt(Vector2Int position, [NotNullWhen(true)] out TObject? ret) {
        // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
        foreach(TObject? obj in _objects)
            if(obj is not null && obj.inLevelInt && obj.position == position) {
                ret = obj;
                return true;
            }
        ret = null;
        return false;
    }
    public bool TryGetObjectAt(Vector2Int position, int minLayer, [NotNullWhen(true)] out TObject? ret) {
        // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
        foreach(TObject? obj in _objects)
            if(obj is not null && obj.inLevelInt && obj.position == position && obj.layer >= minLayer) {
                ret = obj;
                return true;
            }
        ret = null;
        return false;
    }

    public bool TryGetObjectAt<T>(Vector2Int position, [NotNullWhen(true)] out T? ret) where T : class {
        foreach(TObject? obj in _objects)
            if(obj is T objT && obj.inLevelInt && obj.position == position) {
                ret = objT;
                return true;
            }
        ret = null;
        return false;
    }
    public bool TryGetObjectAt<T>(Vector2Int position, int minLayer, [NotNullWhen(true)] out T? ret) where T : class {
        foreach(TObject? obj in _objects)
            if(obj is T objT && obj.inLevelInt && obj.position == position && obj.layer >= minLayer) {
                ret = objT;
                return true;
            }
        ret = null;
        return false;
    }

    public IEnumerable<TObject> GetObjectsAt(Vector2Int position) {
        // ReSharper disable once ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
        foreach(TObject? obj in _objects)
            if(obj is not null && obj.inLevelInt && obj.position == position)
                yield return obj;
    }
    public IEnumerable<TObject> GetObjectsAt(Vector2Int position, int minLayer) {
        // ReSharper disable once ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
        foreach(TObject? obj in _objects)
            if(obj is not null && obj.inLevelInt && obj.position == position && obj.layer >= minLayer)
                yield return obj;
    }

    public IEnumerable<T> GetObjectsAt<T>(Vector2Int position) where T : class {
        foreach(TObject? obj in _objects)
            if(obj is T objT && obj.inLevelInt && obj.position == position)
                yield return objT;
    }
    public IEnumerable<T> GetObjectsAt<T>(Vector2Int position, int minLayer) where T : class {
        foreach(TObject? obj in _objects)
            if(obj is T objT && obj.inLevelInt && obj.position == position && obj.layer >= minLayer)
                yield return objT;
    }
}
