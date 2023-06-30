using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

using PER.Util;

namespace PER.Abstractions.Environment;

public class Chunk<TObject> : IUpdatable, ITickable where TObject : LevelObject<Level<TObject>> {
    private readonly List<TObject> _objects = new();

    public void Add(TObject obj) {
        _objects.Add(obj);
        _objects.Sort((a, b) => a.layer.CompareTo(b.layer));
    }

    public void Remove(TObject obj) => _objects.Remove(obj);

    public void Update(TimeSpan time) {
        foreach(TObject obj in _objects)
            obj.Update(time);
    }

    public void Draw() {
        foreach(TObject obj in _objects)
            obj.Draw();
    }

    public void Tick(TimeSpan time) {
        foreach(TObject obj in _objects)
            obj.Tick(time);
    }

    public bool HasObjectAt(Vector2Int position) {
        // ReSharper disable once ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
        foreach(TObject obj in _objects)
            if(obj.position == position)
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

    public bool HasObjectAt<T>(Vector2Int position) where T : TObject {
        // ReSharper disable once ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
        foreach(TObject obj in _objects)
            if(obj is T && obj.position == position)
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

    public bool TryGetObjectAt<T>(Vector2Int position, [NotNullWhen(true)] out T? ret) where T : TObject {
        // ReSharper disable once ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
        foreach(TObject obj in _objects)
            if(obj is T objT && obj.position == position) {
                ret = objT;
                return true;
            }
        ret = null;
        return false;
    }
}
