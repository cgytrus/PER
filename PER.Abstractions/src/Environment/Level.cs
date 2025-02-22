﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

using JetBrains.Annotations;

using PER.Abstractions.Meta;
using PER.Util;

namespace PER.Abstractions.Environment;

[PublicAPI]
public abstract class Level<TLevel, TChunk, TObject> : IUpdatable, ITickable
    where TLevel : Level<TLevel, TChunk, TObject>
    where TChunk : Chunk<TLevel, TChunk, TObject>, new()
    where TObject : LevelObject<TLevel, TChunk, TObject> {
    public bool isClient { get; }
    public Vector2Int chunkSize { get; }

    public Color ambientLight { get; set; } = new(0.1f, 0.1f, 0.1f, 0.4f);

    public Vector2Int cameraPosition { get; set; }

    public LevelUpdateState updateState { get; private set; }

    public bool doLighting {
        get => _doLighting;
        set {
            _doLighting = value;
            if(value)
                foreach(TObject obj in _objects.Values)
                    _light.QueuePropagate(obj);
            else
                foreach(TObject obj in _objects.Values)
                    _light.QueueReset(obj);
        }
    }
    private bool _doLighting = true;

    private Lighting<TLevel, TChunk, TObject> _light;

    protected abstract TimeSpan maxGenerationTime { get; }
    internal bool shouldGenerateChunks { get; private set; }

    public IReadOnlyDictionary<Guid, TObject> objects => _objects;
    private readonly Dictionary<Guid, TObject> _objects = new();
    internal HashSet<TObject> dirtyObjects { get; } = new();

    private readonly Dictionary<Vector2Int, TChunk> _chunks = new();
    private readonly List<TChunk> _chunkValues = new();
    private readonly Queue<Chunk<TLevel, TChunk, TObject>> _chunksToGenerate = new();
    private readonly Vector2Int _minChunkPos;
    private readonly Vector2Int _maxChunkPos;

    public event Action<TObject>? objectAdded;
    public event Action<TObject>? objectRemoved;
    public event Action<TObject>? objectChanged;

    private readonly Stopwatch _generationTimer = new();

    protected Level(bool isClient, Vector2Int chunkSize, Lighting<TLevel, TChunk, TObject>? lighting = null) {
        this.isClient = isClient;
        this.chunkSize = chunkSize;
        _light = lighting ?? new SimpleLighting<TLevel, TChunk, TObject>();
        _light.SetLevel(this);
        _minChunkPos = LevelToChunkPosition(new Vector2Int(int.MinValue, int.MinValue));
        _maxChunkPos = LevelToChunkPosition(new Vector2Int(int.MaxValue, int.MaxValue));
    }

    public void Add(TObject obj) {
        if(updateState == LevelUpdateState.Update)
            throw new InvalidOperationException("Add cannot be called from Update.");
        _objects.Add(obj.id, obj);
        obj.SetLevel(this);
        TChunk chunk = GetChunkAt(LevelToChunkPosition(obj.position));
        chunk.Add(obj);
        if(obj.blocksLight)
            _light.QueueLitBy(chunk);
        _light.QueuePropagate(obj);
        (obj as IAddable)?.Added();
        objectAdded?.Invoke(obj);
    }

    public void Remove(TObject obj) => Remove(obj.id);
    public void Remove(Guid objId) {
        if(updateState == LevelUpdateState.Update)
            throw new InvalidOperationException("Remove cannot be called from Update.");
        if(!_objects.TryGetValue(objId, out TObject? obj))
            return;
        _light.QueueReset(obj);
        GetChunkAt(LevelToChunkPosition(obj.position)).Remove(obj);
        _light.QueueBlockedBy(obj);
        _objects.Remove(objId);
        (obj as IRemovable)?.Removed();
        obj.SetLevel(null);
        objectRemoved?.Invoke(obj);
    }

    public virtual void Update(TimeSpan time) {
        shouldGenerateChunks = true;
        updateState = LevelUpdateState.Update;
        foreach(TObject obj in dirtyObjects)
            CheckDirty(obj);
        dirtyObjects.Clear();
        updateState = LevelUpdateState.None;
        _light.UpdateQueued();
        updateState = LevelUpdateState.Update;
        Bounds cameraChunks = new(
            ScreenToChunkPosition(-chunkSize / 2),
            ScreenToChunkPosition(renderer.size - new Vector2Int(1, 1) + chunkSize / 2)
        );
        for(int x = cameraChunks.min.x; x != cameraChunks.max.x + 1; x++) {
            if(x > _maxChunkPos.x)
                x = _minChunkPos.x;
            for(int y = cameraChunks.min.y; y != cameraChunks.max.y + 1; y++) {
                if(y > _maxChunkPos.y)
                    y = _minChunkPos.y;
                Vector2Int pos = new(x, y);
                TChunk chunk = GetChunkAt(pos);
                chunk.Update(time);
                chunk.Draw(ChunkToScreenPosition(pos));
            }
        }
        updateState = LevelUpdateState.None;
    }

    public virtual void Tick(TimeSpan time) {
        shouldGenerateChunks = true;
        updateState = LevelUpdateState.Tick;

        // always load chunk 0, 0
        LoadChunkAt(new Vector2Int(0, 0));

        // ReSharper disable once ForCanBeConvertedToForeach
        for(int i = 0; i < _chunkValues.Count; i++) {
            TChunk chunk = _chunkValues[i];
            if(chunk.ticks <= 0)
                continue;
            chunk.ticks--;
            chunk.Tick(time);
        }
        foreach(TObject obj in dirtyObjects)
            CheckDirty(obj);
        dirtyObjects.Clear();

        updateState = LevelUpdateState.None;

        TimeSpan startTime = _generationTimer.time;
        while((maxGenerationTime == TimeSpan.Zero || _generationTimer.time - startTime < maxGenerationTime) &&
            _chunksToGenerate.TryDequeue(out Chunk<TLevel, TChunk, TObject>? chunk)) {
            Vector2Int levelPosition = ChunkToLevelPosition(chunk.position);
            // weird chunk size, skip gen
            if(LevelToChunkPosition(levelPosition + chunkSize - new Vector2Int(1, 1)) != chunk.position)
                continue;
            GenerateChunk(levelPosition);
        }

        _light.UpdateQueued();
    }

    public void CheckDirty(TObject obj) {
        if(obj.positionDirty) {
            Vector2Int fromChunkPos = LevelToChunkPosition(obj.internalPrevPosition);
            Vector2Int toChunkPos = LevelToChunkPosition(obj.position);
            TChunk to = GetChunkAt(toChunkPos);
            if(fromChunkPos != toChunkPos) {
                GetChunkAt(fromChunkPos).Remove(obj);
                to.Add(obj);
            }
            _light.QueueBlockedBy(obj);
            if(obj.blocksLight)
                _light.QueueLitBy(to);
            _light.QueueReset(obj);
            _light.QueuePropagate(obj);
            // ReSharper disable once SuspiciousTypeConversion.Global
            (obj as IMovable)?.Moved(obj.internalPrevPosition);
        }
        else if(obj.lightDirty) {
            _light.QueueReset(obj);
            _light.QueuePropagate(obj);
        }
        if(obj.dirty)
            objectChanged?.Invoke(obj);
        obj.ClearDirty();
    }

    internal void QueueGenerateChunk(Chunk<TLevel, TChunk, TObject> chunk) => _chunksToGenerate.Enqueue(chunk);

    protected abstract void GenerateChunk(Vector2Int start);

    public void LoadChunkAt(Vector2Int chunkPosition) => GetChunkAt(chunkPosition).ticks += 2;
    internal TChunk GetChunkAt(Vector2Int chunkPosition) {
        if(_chunks.TryGetValue(chunkPosition, out TChunk? chunk))
            return chunk;
        chunk = new TChunk();
        chunk.Initialize(this, chunkPosition);
        _chunks.Add(chunkPosition, chunk);
        _chunkValues.Add(chunk);
        return chunk;
    }

    public bool HasObjectAt(Vector2Int position) =>
        GetChunkAt(LevelToChunkPosition(position)).HasObjectAt(position);
    public bool HasObjectAt(Vector2Int position, int minLayer) =>
        GetChunkAt(LevelToChunkPosition(position)).HasObjectAt(position, minLayer);
    public bool HasObjectAt(Vector2Int position, Type type) =>
        GetChunkAt(LevelToChunkPosition(position)).HasObjectAt(position, type);
    public bool HasObjectAt(Vector2Int position, int minLayer, Type type) =>
        GetChunkAt(LevelToChunkPosition(position)).HasObjectAt(position, minLayer, type);
    public bool HasObjectAt<T>(Vector2Int position) where T : class =>
        GetChunkAt(LevelToChunkPosition(position)).HasObjectAt<T>(position);
    public bool HasObjectAt<T>(Vector2Int position, int minLayer) where T : class =>
        GetChunkAt(LevelToChunkPosition(position)).HasObjectAt<T>(position, minLayer);
    public bool TryGetObjectAt(Vector2Int position, [NotNullWhen(true)] out TObject? ret) =>
        GetChunkAt(LevelToChunkPosition(position)).TryGetObjectAt(position, out ret);
    public bool TryGetObjectAt(Vector2Int position, int minLayer, [NotNullWhen(true)] out TObject? ret) =>
        GetChunkAt(LevelToChunkPosition(position)).TryGetObjectAt(position, minLayer, out ret);
    public bool TryGetObjectAt<T>(Vector2Int position, [NotNullWhen(true)] out T? ret) where T : class =>
        GetChunkAt(LevelToChunkPosition(position)).TryGetObjectAt(position, out ret);
    public bool TryGetObjectAt<T>(Vector2Int position, int minLayer, [NotNullWhen(true)] out T? ret) where T : class =>
        GetChunkAt(LevelToChunkPosition(position)).TryGetObjectAt(position, minLayer, out ret);
    public IEnumerable<TObject> GetObjectsAt(Vector2Int position) =>
        GetChunkAt(LevelToChunkPosition(position)).GetObjectsAt(position);
    public IEnumerable<TObject> GetObjectsAt(Vector2Int position, int minLayer) =>
        GetChunkAt(LevelToChunkPosition(position)).GetObjectsAt(position, minLayer);
    public IEnumerable<T> GetObjectsAt<T>(Vector2Int position) where T : class =>
        GetChunkAt(LevelToChunkPosition(position)).GetObjectsAt<T>(position);
    public IEnumerable<T> GetObjectsAt<T>(Vector2Int position, int minLayer) where T : class =>
        GetChunkAt(LevelToChunkPosition(position)).GetObjectsAt<T>(position, minLayer);

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public Vector2Int LevelToCameraPosition(Vector2Int levelPosition) => levelPosition - cameraPosition;
    [RequiresHead, MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public Vector2Int LevelToScreenPosition(Vector2Int levelPosition) =>
        CameraToScreenPosition(LevelToCameraPosition(levelPosition));
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public Vector2Int LevelToChunkPosition(Vector2Int levelPosition) =>
        new(Meth.FloorDiv(levelPosition.x, chunkSize.x), Meth.FloorDiv(levelPosition.y, chunkSize.y));
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public Vector2Int LevelToInChunkPosition(Vector2Int levelPosition) =>
        new(Meth.Mod(levelPosition.x, chunkSize.x), Meth.Mod(levelPosition.y, chunkSize.y));

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public Vector2Int CameraToLevelPosition(Vector2Int cameraPosition) => cameraPosition + this.cameraPosition;
    [RequiresHead, MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public Vector2Int CameraToScreenPosition(Vector2Int cameraPosition) =>
        isClient ? cameraPosition + renderer.size / 2 :
            throw new InvalidOperationException("Screen accessed from server");
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public Vector2Int CameraToChunkPosition(Vector2Int cameraPosition) =>
        LevelToChunkPosition(CameraToLevelPosition(cameraPosition));

    [RequiresHead, MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public Vector2Int ScreenToLevelPosition(Vector2Int screenPosition) =>
        ScreenToCameraPosition(CameraToLevelPosition(screenPosition));
    [RequiresHead, MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public Vector2Int ScreenToCameraPosition(Vector2Int screenPosition) =>
        isClient ? screenPosition - renderer.size / 2 :
            throw new InvalidOperationException("Screen accessed from server");
    [RequiresHead, MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public Vector2Int ScreenToChunkPosition(Vector2Int screenPosition) =>
        LevelToChunkPosition(ScreenToLevelPosition(screenPosition));

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public Vector2Int ChunkToLevelPosition(Vector2Int chunkPosition) =>
        new(chunkPosition.x * chunkSize.x, chunkPosition.y * chunkSize.y);
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public Vector2Int ChunkToCameraPosition(Vector2Int chunkPosition) =>
        LevelToCameraPosition(ChunkToLevelPosition(chunkPosition));
    [RequiresHead, MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public Vector2Int ChunkToScreenPosition(Vector2Int chunkPosition) =>
        LevelToScreenPosition(ChunkToLevelPosition(chunkPosition));

    public Bounds GetBounds() {
        int minX = int.MaxValue;
        int minY = int.MaxValue;
        int maxX = int.MinValue;
        int maxY = int.MinValue;
        foreach(TObject obj in _objects.Values) {
            if(obj.position.x < minX)
                minX = obj.position.x;
            if(obj.position.y < minY)
                minY = obj.position.y;
            if(obj.position.x > maxX)
                maxX = obj.position.x;
            if(obj.position.y > maxY)
                maxY = obj.position.y;
        }
        return new Bounds(new Vector2Int(minX, minY), new Vector2Int(maxX, maxY));
    }
}
