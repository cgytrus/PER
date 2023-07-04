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

    private readonly List<TObject> _objects = new();
    private readonly List<IUpdatable> _updatables = new();
    private readonly List<ITickable> _tickables = new();

    public void Add(TObject obj) {
        _objects.Add(obj);
        _objects.Sort((a, b) => a.layer.CompareTo(b.layer));
        if(obj is IUpdatable updatable)
            _updatables.Add(updatable);
        if(obj is ITickable tickable)
            _tickables.Add(tickable);
    }

    public void Remove(TObject obj) {
        _objects.Remove(obj);
        if(obj is IUpdatable updatable)
            _updatables.Remove(updatable);
        if(obj is ITickable tickable)
            _tickables.Remove(tickable);
    }

    public void Update(TimeSpan time) {
        foreach(IUpdatable updatable in _updatables)
            updatable.Update(time);
    }

    public void Draw() {
        foreach(TObject obj in _objects)
            obj.Draw();
    }

    public void Tick(TimeSpan time) {
        foreach(ITickable tickable in _tickables)
            tickable.Tick(time);
    }

    public void PopulateDirty(List<TObject> dirtyObjects) {
        // ReSharper disable once ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
        foreach(TObject obj in _objects)
            if(obj.positionDirty || obj.dirty)
                dirtyObjects.Add(obj);
    }

    public bool HasObjectAt(Vector2Int position) {
        // ReSharper disable once ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
        foreach(TObject obj in _objects)
            if(obj.position == position)
                return true;
        return false;
    }
    public bool HasObjectAt(Vector2Int position, int minLayer) {
        // ReSharper disable once ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
        foreach(TObject obj in _objects)
            if(obj.position == position && obj.layer >= minLayer)
                return true;
        return false;
    }

    public bool HasObjectAt(Vector2Int position, Type type) {
        // ReSharper disable once ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
        foreach(TObject obj in _objects)
            if(obj.GetType() == type && obj.position == position)
                return true;
        return false;
    }
    public bool HasObjectAt(Vector2Int position, int minLayer, Type type) {
        // ReSharper disable once ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
        foreach(TObject obj in _objects)
            if(obj.GetType() == type && obj.position == position && obj.layer >= minLayer)
                return true;
        return false;
    }

    public bool HasObjectAt<T>(Vector2Int position) where T : class {
        // ReSharper disable once ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
        foreach(TObject obj in _objects)
            if(obj is T && obj.position == position)
                return true;
        return false;
    }
    public bool HasObjectAt<T>(Vector2Int position, int minLayer) where T : class {
        // ReSharper disable once ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
        foreach(TObject obj in _objects)
            if(obj is T && obj.position == position && obj.layer >= minLayer)
                return true;
        return false;
    }

    public bool TryGetObjectAt(Vector2Int position, [NotNullWhen(true)] out TObject? ret) {
        // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
        foreach(TObject obj in _objects)
            if(obj.position == position) {
                ret = obj;
                return true;
            }
        ret = null;
        return false;
    }
    public bool TryGetObjectAt(Vector2Int position, int minLayer, [NotNullWhen(true)] out TObject? ret) {
        // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
        foreach(TObject obj in _objects)
            if(obj.position == position && obj.layer >= minLayer) {
                ret = obj;
                return true;
            }
        ret = null;
        return false;
    }

    public bool TryGetObjectAt<T>(Vector2Int position, [NotNullWhen(true)] out T? ret) where T : class {
        foreach(TObject obj in _objects)
            if(obj is T objT && obj.position == position) {
                ret = objT;
                return true;
            }
        ret = null;
        return false;
    }
    public bool TryGetObjectAt<T>(Vector2Int position, int minLayer, [NotNullWhen(true)] out T? ret) where T : class {
        foreach(TObject obj in _objects)
            if(obj is T objT && obj.position == position && obj.layer >= minLayer) {
                ret = objT;
                return true;
            }
        ret = null;
        return false;
    }
}
