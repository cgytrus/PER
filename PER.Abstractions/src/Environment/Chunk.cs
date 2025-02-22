﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

using JetBrains.Annotations;
using PER.Abstractions.Meta;
using PER.Abstractions.Rendering;
using PER.Util;

namespace PER.Abstractions.Environment;

[PublicAPI]
public abstract class Chunk<TLevel, TChunk, TObject> : IUpdatable, ITickable
    where TLevel : Level<TLevel, TChunk, TObject>
    where TChunk : Chunk<TLevel, TChunk, TObject>, new()
    where TObject : LevelObject<TLevel, TChunk, TObject> {
    public Vector2Int position { get; private set; }

    internal int ticks { get; set; }

    protected abstract bool shouldUpdate { get; }
    protected abstract bool shouldTick { get; }

    protected TLevel level => _level!;
    private TLevel? _level;

    internal HashSet<ILight> litBy { get; } = new();

    public Color[,] lighting { get; private set; } = new Color[0, 0];
    public float totalVisibility { get; set; }

    private readonly List<TObject?> _objects = new();
    private readonly List<IUpdatable?> _updatables = new();
    private readonly List<ITickable?> _tickables = new();

    private bool _generated;

    private bool _shouldProcessRemoved;

    internal void Initialize(Level<TLevel, TChunk, TObject>? level, Vector2Int position) {
        _level = level as TLevel;
        if(level is null)
            return;
        this.position = position;
        lighting = new Color[level.chunkSize.y, level.chunkSize.x];
        totalVisibility = 0f;
        if(!level.shouldGenerateChunks)
            _generated = true;
    }

    public void Add(TObject obj) {
        _objects.Add(obj);
        _objects.Sort((a, b) => (a?.layer ?? int.MinValue).CompareTo(b?.layer ?? int.MinValue));
        if(shouldUpdate && obj is IUpdatable updatable)
            _updatables.Add(updatable);
        if(shouldTick && obj is ITickable tickable)
            _tickables.Add(tickable);
        _shouldProcessRemoved = true;
    }

    public void Remove(TObject obj) {
        int index = _objects.IndexOf(obj);
        if(index >= 0)
            _objects[index] = null;
        if(shouldUpdate && obj is IUpdatable updatable) {
            index = _updatables.IndexOf(updatable);
            if(index >= 0)
                _updatables[index] = null;
        }
        // ReSharper disable once InvertIf
        if(shouldTick && obj is ITickable tickable) {
            index = _tickables.IndexOf(tickable);
            if(index >= 0)
                _tickables[index] = null;
        }
        _shouldProcessRemoved = true;
    }

    public void Update(TimeSpan time) {
        if (!shouldUpdate)
            return;
        // ReSharper disable once ForCanBeConvertedToForeach
        for (int i = 0; i < _updatables.Count; i++) {
            IUpdatable? updatable = _updatables[i];
            if (updatable is TObject { inLevelInt: true })
                updatable.Update(time);
        }
        ProcessRemoved();
    }

    [RequiresHead]
    public void Draw(Vector2Int start) {
        if (!shouldUpdate || level.doLighting && totalVisibility == 0f)
            return;
        // ReSharper disable once ForCanBeConvertedToForeach
        for(int i = 0; i < _objects.Count; i++) {
            TObject? obj = _objects[i];
            if(obj is null || !obj.inLevelInt)
                continue;
            Vector2Int screenPos = level.LevelToScreenPosition(obj.position);
            Vector2Int localPos = screenPos - start;
            if(level.doLighting && lighting[localPos.y, localPos.x].a == 0f)
                continue;
            renderer.DrawCharacter(screenPos, ApplyLight(obj.character, localPos), obj.effect);
        }
    }

    private RenderCharacter ApplyLight(RenderCharacter c, Vector2Int pos) {
        if(!level.doLighting)
            return c;
        float v = Math.Min(lighting[pos.y, pos.x].a * (1f + level.ambientLight.a), 1f);
        Color3 l = (Color3)lighting[pos.y, pos.x] + (Color3)level.ambientLight;
        Color final = new(l.r * v, l.g * v, l.b * v);
        return c with { background = c.background * final, foreground = c.foreground * final };
    }

    public void Tick(TimeSpan time) {
        if(!shouldTick)
            return;
        if(!_generated) {
            level.QueueGenerateChunk(this);
            _generated = true;
        }
        // ReSharper disable once ForCanBeConvertedToForeach
        for(int i = 0; i < _tickables.Count; i++) {
            ITickable? tickable = _tickables[i];
            if(tickable is TObject { inLevelInt: true })
                tickable.Tick(time);
        }
        ProcessRemoved();
    }

    private void ProcessRemoved() {
        if(!_shouldProcessRemoved)
            return;
        _shouldProcessRemoved = false;
        _objects.RemoveAll(x => x is null || !x.inLevelInt);
        if(shouldUpdate)
            _updatables.RemoveAll(x => x is null or TObject { inLevelInt: false });
        if(shouldTick)
            _tickables.RemoveAll(x => x is null or TObject { inLevelInt: false });
    }

    public bool HasObjectAt(Vector2Int position) {
        for(int i = _objects.Count - 1; i >= 0; i--) {
            TObject? obj = _objects[i];
            if(obj is not null && obj.inLevelInt && obj.position == position)
                return true;
        }
        return false;
    }
    public bool HasObjectAt(Vector2Int position, int minLayer) {
        for(int i = _objects.Count - 1; i >= 0; i--) {
            TObject? obj = _objects[i];
            if(obj is not null && obj.inLevelInt && obj.position == position && obj.layer >= minLayer)
                return true;
        }
        return false;
    }

    public bool HasObjectAt(Vector2Int position, Type type) {
        for(int i = _objects.Count - 1; i >= 0; i--) {
            TObject? obj = _objects[i];
            if(obj is not null && obj.inLevelInt && obj.GetType() == type && obj.position == position)
                return true;
        }
        return false;
    }
    public bool HasObjectAt(Vector2Int position, int minLayer, Type type) {
        for(int i = _objects.Count - 1; i >= 0; i--) {
            TObject? obj = _objects[i];
            if(obj is not null && obj.inLevelInt && obj.GetType() == type && obj.position == position &&
                obj.layer >= minLayer)
                return true;
        }
        return false;
    }

    public bool HasObjectAt<T>(Vector2Int position) where T : class {
        for(int i = _objects.Count - 1; i >= 0; i--) {
            TObject? obj = _objects[i];
            if(obj is T && obj.inLevelInt && obj.position == position)
                return true;
        }
        return false;
    }
    public bool HasObjectAt<T>(Vector2Int position, int minLayer) where T : class {
        for(int i = _objects.Count - 1; i >= 0; i--) {
            TObject? obj = _objects[i];
            if(obj is T && obj.inLevelInt && obj.position == position && obj.layer >= minLayer)
                return true;
        }
        return false;
    }

    public bool TryGetObjectAt(Vector2Int position, [NotNullWhen(true)] out TObject? ret) {
        for(int i = _objects.Count - 1; i >= 0; i--) {
            TObject? obj = _objects[i];
            if(obj is null || !obj.inLevelInt || obj.position != position)
                continue;
            ret = obj;
            return true;
        }
        ret = null;
        return false;
    }
    public bool TryGetObjectAt(Vector2Int position, int minLayer, [NotNullWhen(true)] out TObject? ret) {
        for(int i = _objects.Count - 1; i >= 0; i--) {
            TObject? obj = _objects[i];
            if(obj is null || !obj.inLevelInt || obj.position != position || obj.layer < minLayer)
                continue;
            ret = obj;
            return true;
        }
        ret = null;
        return false;
    }

    public bool TryGetObjectAt<T>(Vector2Int position, [NotNullWhen(true)] out T? ret) where T : class {
        for(int i = _objects.Count - 1; i >= 0; i--) {
            TObject? obj = _objects[i];
            if(obj is not T objT || !obj.inLevelInt || obj.position != position)
                continue;
            ret = objT;
            return true;
        }
        ret = null;
        return false;
    }
    public bool TryGetObjectAt<T>(Vector2Int position, int minLayer, [NotNullWhen(true)] out T? ret) where T : class {
        for(int i = _objects.Count - 1; i >= 0; i--) {
            TObject? obj = _objects[i];
            if(obj is not T objT || !obj.inLevelInt || obj.position != position || obj.layer < minLayer)
                continue;
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

    internal bool TryMarkBlockingLightAt(Vector2Int position, ILight light, out Color color) {
        bool res = false;
        color = Color.transparent;
        // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
        foreach(TObject? obj in _objects) {
            if(obj is null || !obj.inLevelInt || obj.position != position || !obj.blocksLight)
                continue;
            res = true;
            obj.blockedLights?.Add(light);
            color = obj.character.foreground;
        }
        return res;
    }
    internal void MarkNotBlockingLightAt(Vector2Int position, ILight light) {
        // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
        foreach(TObject? obj in _objects)
            if(obj is not null && obj.inLevelInt && obj.position == position && obj.blocksLight)
                obj.blockedLights?.Remove(light);
    }
}
