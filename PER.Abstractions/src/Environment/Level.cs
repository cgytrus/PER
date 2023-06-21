using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

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

    private Dictionary<Vector2Int, HashSet<LevelObject>> _tracked = new();
    private List<Vector2Int> _trackedToRemove = new();

    public Level(IRenderer renderer, IInput input, IAudio audio, IResources resources) {
        this.renderer = renderer;
        this.input = input;
        this.audio = audio;
        this.resources = resources;
    }

    public void Add(LevelObject obj) {
        _objects.Add(obj);
        TrackObject(obj);
    }

    public void Remove(LevelObject obj) {
        _objects.Remove(obj);
    }

    public void Update(TimeSpan time) {
        foreach(Vector2Int position in _trackedToRemove)
            _tracked.Remove(position);
        _trackedToRemove.Clear();

        current = this;
        foreach(LevelObject obj in objects)
            obj.Update(time);
        foreach(LevelObject obj in objects)
            obj.Draw();
        current = null;
    }

    public void Tick(TimeSpan time) {
        current = this;
        foreach(LevelObject obj in objects)
            obj.Tick(time);
        current = null;
    }

    public IReadOnlySet<LevelObject> GetObjectsAt(Vector2Int position) =>
        _tracked.TryGetValue(position, out HashSet<LevelObject>? objs) ? objs : ImmutableHashSet<LevelObject>.Empty;

    // the tracking code is funny and idk of a better way to do it xd
    internal void ObjectMoved(LevelObject obj, Vector2Int from, Vector2Int to) {
        if(_tracked.TryGetValue(from, out HashSet<LevelObject>? objs)) {
            objs.Remove(obj);
            if(objs.Count == 0)
                _trackedToRemove.Add(from);
        }
        if(!_tracked.TryGetValue(to, out objs)) {
            objs = new HashSet<LevelObject>();
            _tracked[to] = objs;
        }
        objs.Add(obj);
    }

    private void TrackObject(LevelObject obj) {
        if(!_tracked.TryGetValue(obj.position, out HashSet<LevelObject>? objs)) {
            objs = new HashSet<LevelObject>();
            _tracked[obj.position] = objs;
        }
        objs.Add(obj);
    }
}
