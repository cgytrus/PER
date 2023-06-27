using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

using JetBrains.Annotations;

using PER.Abstractions.Audio;
using PER.Abstractions.Input;
using PER.Abstractions.Rendering;
using PER.Abstractions.Resources;
using PER.Util;

namespace PER.Abstractions.Environment;

public abstract class Level : IUpdatable, ITickable {
    public static Level? current { get; protected set; }

    public IRenderer renderer { get; }
    public IInput input { get; }
    public IAudio audio { get; }
    public IResources resources { get; }

    public Vector2Int cameraPosition { get; set; }

    protected Level(IRenderer renderer, IInput input, IAudio audio, IResources resources) {
        this.renderer = renderer;
        this.input = input;
        this.audio = audio;
        this.resources = resources;
    }

    public virtual void Reset() {
        cameraPosition = new Vector2Int();
    }

    public abstract void Update(TimeSpan time);
    public abstract void Tick(TimeSpan time);

    public abstract void Sort();

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public Vector2Int LevelToCameraPosition(Vector2Int levelPosition) => levelPosition - cameraPosition;
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public Vector2Int LevelToScreenPosition(Vector2Int levelPosition) =>
        CameraToScreenPosition(LevelToCameraPosition(levelPosition));

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public Vector2Int CameraToLevelPosition(Vector2Int cameraPosition) => cameraPosition + this.cameraPosition;
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public Vector2Int CameraToScreenPosition(Vector2Int cameraPosition) => cameraPosition + renderer.size / 2;

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public Vector2Int ScreenToLevelPosition(Vector2Int screenPosition) =>
        ScreenToCameraPosition(CameraToLevelPosition(screenPosition));
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public Vector2Int ScreenToCameraPosition(Vector2Int screenPosition) => screenPosition - renderer.size / 2;
}

[PublicAPI]
public class Level<TObject> : Level where TObject : LevelObject<Level<TObject>> {
    public IReadOnlyDictionary<Guid, TObject> objects => _objects;
    private readonly Dictionary<Guid, TObject> _objects = new();
    private readonly List<TObject> _orderedObjects = new();

    public event Action<TObject>? objectAdded;
    public event Action<TObject>? objectRemoved;
    public event Action<TObject>? objectChanged;

    public Level(IRenderer renderer, IInput input, IAudio audio, IResources resources) :
        base(renderer, input, audio, resources) { }

    public override void Reset() {
        base.Reset();
        _objects.Clear();
        _orderedObjects.Clear();
    }

    public void Add(TObject obj) {
        _objects.Add(obj.id, obj);
        _orderedObjects.Add(obj);
        Sort();
        obj.added = true;
        objectAdded?.Invoke(obj);
    }

    public void Remove(Guid objId) {
        if(!_objects.TryGetValue(objId, out TObject? obj))
            return;
        _objects.Remove(objId);
        _orderedObjects.Remove(obj);
        objectRemoved?.Invoke(obj);
    }

    public void Remove(TObject obj) {
        _objects.Remove(obj.id);
        _orderedObjects.Remove(obj);
        objectRemoved?.Invoke(obj);
    }

    public override void Update(TimeSpan time) {
        current = this;
        foreach(TObject obj in _objects.Values)
            obj.Update(time);
        foreach(TObject obj in _orderedObjects)
            obj.Draw();
        CheckDirtyObjects();
        current = null;
    }

    public override void Tick(TimeSpan time) {
        current = this;
        foreach(TObject obj in _objects.Values)
            obj.Tick(time);
        CheckDirtyObjects();
        current = null;
    }

    private void CheckDirtyObjects() {
        // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
        foreach(TObject obj in _objects.Values) {
            if(!obj.dirty)
                continue;
            objectChanged?.Invoke(obj);
            obj.ClearDirty();
        }
    }

    public override void Sort() => _orderedObjects.Sort((a, b) => a.layer.CompareTo(b.layer));

    public bool HasObjectAt(Vector2Int position) {
        // ReSharper disable once ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
        foreach(TObject obj in _objects.Values)
            if(obj.position == position)
                return true;
        return false;
    }

    public bool HasObjectAt(Vector2Int position, Type type) {
        // ReSharper disable once ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
        foreach(TObject obj in _objects.Values)
            if(obj.GetType() == type && obj.position == position)
                return true;
        return false;
    }

    public bool HasObjectAt<T>(Vector2Int position) where T : TObject {
        // ReSharper disable once ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
        foreach(TObject obj in _objects.Values)
            if(obj is T && obj.position == position)
                return true;
        return false;
    }

    public bool TryGetObjectAt(Vector2Int position, [NotNullWhen(true)] out TObject? ret) {
        // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
        foreach(TObject obj in _objects.Values)
            if(obj.position == position) {
                ret = obj;
                return true;
            }
        ret = null;
        return false;
    }

    public bool TryGetObjectAt<T>(Vector2Int position, [NotNullWhen(true)] out T? ret) where T : TObject {
        // ReSharper disable once ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
        foreach(TObject obj in _objects.Values)
            if(obj is T objT && obj.position == position) {
                ret = objT;
                return true;
            }
        ret = null;
        return false;
    }
}
