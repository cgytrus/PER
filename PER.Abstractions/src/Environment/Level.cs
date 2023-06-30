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

[PublicAPI]
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

    private readonly Vector2Int _chunkSize;
    private readonly Dictionary<Vector2Int, Chunk<TObject>> _chunks = new();
    private readonly Dictionary<Vector2Int, Chunk<TObject>> _newChunks = new();
    private readonly List<Bounds> _chunksToGenerate = new();
    private readonly Vector2Int _minChunkPos;
    private readonly Vector2Int _maxChunkPos;

    public event Action<TObject>? objectAdded;
    public event Action<TObject>? objectRemoved;
    public event Action<TObject>? objectChanged;
    public event Action<Bounds>? chunkCreated;

    public Level(IRenderer renderer, IInput input, IAudio audio, IResources resources, Vector2Int chunkSize) :
        base(renderer, input, audio, resources) {
        _chunkSize = chunkSize;
        _minChunkPos = LevelToChunkPosition(new Vector2Int(int.MinValue, int.MinValue));
        _maxChunkPos = LevelToChunkPosition(new Vector2Int(int.MaxValue, int.MaxValue));
    }

    public override void Reset() {
        base.Reset();
        _objects.Clear();
        _chunks.Clear();
    }

    public void Add(TObject obj) {
        _objects.Add(obj.id, obj);
        GetChunkAt(LevelToChunkPosition(obj.position)).Add(obj);
        current = this;
        obj.Added();
        current = null;
        objectAdded?.Invoke(obj);
    }

    public void Remove(TObject obj) => Remove(obj.id);
    public void Remove(Guid objId) {
        if(!_objects.TryGetValue(objId, out TObject? obj))
            return;
        _objects.Remove(objId);
        GetChunkAt(LevelToChunkPosition(obj.position)).Remove(obj);
        current = this;
        obj.Removed();
        current = null;
        objectRemoved?.Invoke(obj);
    }

    public override void Update(TimeSpan time) {
        current = this;
        Bounds cameraChunks = new(
            ScreenToChunkPosition(-_chunkSize / 2),
            ScreenToChunkPosition(renderer.size - new Vector2Int(1, 1) + _chunkSize / 2)
        );
        UpdateChunksInBounds(time, cameraChunks);
        CheckDirty();
        DrawChunksInBounds(cameraChunks);
        current = null;
    }

    public override void Tick(TimeSpan time) {
        current = this;
        TickChunks(time);
        CheckDirty();
        current = null;
    }

    private void UpdateChunksInBounds(TimeSpan time, Bounds bounds) {
        for(int x = bounds.min.x; x != bounds.max.x + 1; x++) {
            if(x > _maxChunkPos.x)
                x = _minChunkPos.x;
            for(int y = bounds.min.y; y != bounds.max.y + 1; y++) {
                if(y > _maxChunkPos.y)
                    y = _minChunkPos.y;
                GetChunkAt(new Vector2Int(x, y)).Update(time);
            }
        }
    }

    private void DrawChunksInBounds(Bounds bounds) {
        for(int x = bounds.min.x; x != bounds.max.x + 1; x++) {
            if(x > _maxChunkPos.x)
                x = _minChunkPos.x;
            for(int y = bounds.min.y; y != bounds.max.y + 1; y++) {
                if(y > _maxChunkPos.y)
                    y = _minChunkPos.y;
                GetChunkAt(new Vector2Int(x, y)).Draw();
            }
        }
    }

    private void TickChunks(TimeSpan time) {
        foreach(Chunk<TObject> chunk in _chunks.Values)
            chunk.Tick(time);
    }

    private void CheckDirty() {
        // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
        foreach(TObject obj in _objects.Values) {
            if(obj.positionDirty) {
                Vector2Int fromChunkPos = LevelToChunkPosition(obj.internalPrevPosition);
                Vector2Int toChunkPos = LevelToChunkPosition(obj.position);
                if(fromChunkPos != toChunkPos) {
                    GetChunkAt(fromChunkPos).Remove(obj);
                    GetChunkAt(toChunkPos).Add(obj);
                }
                obj.positionDirty = false;
            }
            if(!obj.dirty)
                continue;
            objectChanged?.Invoke(obj);
            obj.ClearDirty();
        }
        foreach((Vector2Int pos, Chunk<TObject> chunk) in _newChunks) {
            _chunks.Add(pos, chunk);
            Vector2Int levelPosition = ChunkToLevelPosition(pos);
            _chunksToGenerate.Add(new Bounds(levelPosition, levelPosition + _chunkSize - new Vector2Int(1, 1)));
        }
        _newChunks.Clear();
        foreach(Bounds bounds in _chunksToGenerate)
            chunkCreated?.Invoke(bounds);
        _chunksToGenerate.Clear();
    }

    public bool HasObjectAt(Vector2Int position) =>
        GetChunkAt(LevelToChunkPosition(position)).HasObjectAt(position);
    public bool HasObjectAt(Vector2Int position, Type type) =>
        GetChunkAt(LevelToChunkPosition(position)).HasObjectAt(position, type);
    public bool HasObjectAt<T>(Vector2Int position) where T : TObject =>
        GetChunkAt(LevelToChunkPosition(position)).HasObjectAt<T>(position);
    public bool TryGetObjectAt(Vector2Int position, [NotNullWhen(true)] out TObject? ret) =>
        GetChunkAt(LevelToChunkPosition(position)).TryGetObjectAt(position, out ret);
    public bool TryGetObjectAt<T>(Vector2Int position, [NotNullWhen(true)] out T? ret) where T : TObject =>
        GetChunkAt(LevelToChunkPosition(position)).TryGetObjectAt(position, out ret);

    public void CreateChunkAt(Vector2Int chunkPosition) => GetChunkAt(chunkPosition);
    private Chunk<TObject> GetChunkAt(Vector2Int chunkPosition) {
        if(_chunks.TryGetValue(chunkPosition, out Chunk<TObject>? chunk) ||
            _newChunks.TryGetValue(chunkPosition, out chunk))
            return chunk;
        chunk = new Chunk<TObject>();
        _newChunks.Add(chunkPosition, chunk);
        return chunk;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public Vector2Int LevelToChunkPosition(Vector2Int levelPosition) {
        // all my homies hate negative numbers
        int x = (int)MathF.Floor(levelPosition.x / (float)_chunkSize.x);
        int y = (int)MathF.Floor(levelPosition.y / (float)_chunkSize.y);
        return new Vector2Int(x, y);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public Vector2Int CameraToChunkPosition(Vector2Int cameraPosition) =>
        LevelToChunkPosition(CameraToLevelPosition(cameraPosition));
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public Vector2Int ScreenToChunkPosition(Vector2Int screenPosition) =>
        LevelToChunkPosition(ScreenToLevelPosition(screenPosition));

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public Vector2Int ChunkToLevelPosition(Vector2Int chunkPosition) =>
        new(chunkPosition.x * _chunkSize.x, chunkPosition.y * _chunkSize.y);
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public Vector2Int ChunkToCameraPosition(Vector2Int chunkPosition) =>
        LevelToCameraPosition(ChunkToLevelPosition(chunkPosition));
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public Vector2Int ChunkToScreenPosition(Vector2Int chunkPosition) =>
        LevelToScreenPosition(ChunkToLevelPosition(chunkPosition));
}
