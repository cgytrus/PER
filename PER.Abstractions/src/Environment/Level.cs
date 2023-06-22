using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

using JetBrains.Annotations;

using PER.Abstractions.Audio;
using PER.Abstractions.Input;
using PER.Abstractions.Rendering;
using PER.Abstractions.Resources;
using PER.Util;

namespace PER.Abstractions.Environment;

[PublicAPI]
public class Level : IUpdatable, ITickable {
    public static Level? current { get; private set; }

    public IRenderer renderer { get; }
    public IInput input { get; }
    public IAudio audio { get; }
    public IResources resources { get; }

    public IReadOnlyList<LevelObject> objects => _objects;
    private readonly List<LevelObject> _objects = new();

    public Vector2Int cameraPosition { get; set; }

    public Level(IRenderer renderer, IInput input, IAudio audio, IResources resources) {
        this.renderer = renderer;
        this.input = input;
        this.audio = audio;
        this.resources = resources;
    }

    public void Add(LevelObject obj) {
        _objects.Add(obj);
    }

    public void Remove(LevelObject obj) {
        _objects.Remove(obj);
    }

    public void Update(TimeSpan time) {
        current = this;
        foreach(LevelObject obj in _objects)
            obj.Update(time);
        foreach(LevelObject obj in _objects)
            obj.Draw();
        current = null;
    }

    public void Tick(TimeSpan time) {
        current = this;
        foreach(LevelObject obj in _objects)
            obj.Tick(time);
        current = null;
    }

    public bool HasObjectAt(Vector2Int position) {
        // ReSharper disable once ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
        foreach(LevelObject obj in _objects)
            if(obj.position == position)
                return true;
        return false;
    }

    public bool HasObjectAt<T>(Vector2Int position) where T : LevelObject {
        // ReSharper disable once ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
        foreach(LevelObject obj in _objects)
            if(obj is T && obj.position == position)
                return true;
        return false;
    }

    public bool TryGetObjectAt(Vector2Int position, [NotNullWhen(true)] out LevelObject? ret) {
        // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
        foreach(LevelObject obj in _objects)
            if(obj.position == position) {
                ret = obj;
                return true;
            }
        ret = null;
        return false;
    }

    public bool TryGetObjectAt<T>(Vector2Int position, [NotNullWhen(true)] out T? ret) where T : LevelObject {
        // ReSharper disable once ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
        foreach(LevelObject obj in _objects)
            if(obj is T objT && obj.position == position) {
                ret = objT;
                return true;
            }
        ret = null;
        return false;
    }
}
