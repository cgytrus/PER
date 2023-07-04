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
public abstract class Level<TLevel, TChunk, TObject> : IUpdatable, ITickable
    where TLevel : Level<TLevel, TChunk, TObject>
    where TChunk : Chunk<TLevel, TChunk, TObject>, new()
    where TObject : LevelObject<TLevel, TChunk, TObject> {
    public IRenderer renderer { get; }
    public IInput input { get; }
    public IAudio audio { get; }
    public IResources resources { get; }
    public Vector2Int chunkSize { get; }

    public Vector2Int cameraPosition { get; set; }

    public LevelUpdateState updateState { get; private set; }

    public IReadOnlyDictionary<Guid, TObject> objects => _objects;
    private readonly Dictionary<Guid, TObject> _objects = new();
    private readonly List<TObject> _dirtyObjects = new();

    private readonly Dictionary<Vector2Int, TChunk> _chunks = new();
    private readonly Dictionary<Vector2Int, TChunk> _autoChunks = new();
    private readonly List<(Vector2Int, Vector2Int)> _chunksToGenerate = new();
    private readonly List<Vector2Int> _relightQueue = new();
    private readonly Vector2Int _minChunkPos;
    private readonly Vector2Int _maxChunkPos;

    public event Action<TObject>? objectAdded;
    public event Action<TObject>? objectRemoved;
    public event Action<TObject>? objectChanged;
    public event Action<Vector2Int, Vector2Int>? chunkCreated;

    protected Level(IRenderer renderer, IInput input, IAudio audio, IResources resources, Vector2Int chunkSize) {
        this.renderer = renderer;
        this.input = input;
        this.audio = audio;
        this.resources = resources;
        this.chunkSize = chunkSize;
        _minChunkPos = LevelToChunkPosition(new Vector2Int(int.MinValue, int.MinValue));
        _maxChunkPos = LevelToChunkPosition(new Vector2Int(int.MaxValue, int.MaxValue));
    }

    public void Reset() {
        cameraPosition = new Vector2Int();
        _objects.Clear();
        _chunks.Clear();
    }

    public void Add(TObject obj) {
        _objects.Add(obj.id, obj);
        TChunk chunk = GetChunkAt(LevelToChunkPosition(obj.position));
        chunk.Add(obj);
        foreach(ILight? light in chunk.lights)
            if(light is TObject { inLevelInt: true })
                TryQueueLight(obj.position, light);
        obj.SetLevel(this);
        // ReSharper disable once SuspiciousTypeConversion.Global
        if(obj is IAddable addable)
            addable.Added();
        objectAdded?.Invoke(obj);
    }

    public void Remove(TObject obj) => Remove(obj.id);
    public void Remove(Guid objId) {
        if(!_objects.TryGetValue(objId, out TObject? obj))
            return;
        _objects.Remove(objId);
        TChunk chunk = GetChunkAt(LevelToChunkPosition(obj.position));
        chunk.Remove(obj);
        foreach(ILight? light in chunk.lights)
            if(light is TObject { inLevelInt: true })
                TryQueueLight(obj.position, light);
        if(obj is IRemovable removable)
            removable.Removed();
        obj.SetLevel(null);
        objectRemoved?.Invoke(obj);
    }

    private void TryQueueLight(Vector2Int pos, ILight light) {
        byte dist = Math.Max(light.emission, light.visibility);
        for(int y = -dist; y <= dist; y++) {
            for(int x = -dist; x <= dist; x++) {
                Vector2Int chunkPos = LevelToChunkPosition(pos + new Vector2Int(x, y));
                if(!_relightQueue.Contains(chunkPos))
                    _relightQueue.Add(chunkPos);
            }
        }
    }

    public void Update(TimeSpan time) {
        updateState = LevelUpdateState.Update;
        Bounds cameraChunks = new(
            ScreenToChunkPosition(-chunkSize / 2),
            ScreenToChunkPosition(renderer.size - new Vector2Int(1, 1) + chunkSize / 2)
        );
        foreach(TChunk chunk in _chunks.Values)
            chunk.PopulateDirty(_dirtyObjects);
        // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
        foreach(TObject obj in _dirtyObjects)
            CheckDirty(obj);
        _dirtyObjects.Clear();
        AddNewChunks();
        foreach(Vector2Int pos in _relightQueue)
            GetChunkAt(pos).ClearLighting();
        foreach(Vector2Int pos in _relightQueue)
            GetChunkAt(pos).UpdateLighting();
        _relightQueue.Clear();
        UpdateChunksInBounds(time, cameraChunks);
        updateState = LevelUpdateState.Draw;
        DrawChunksInBounds(cameraChunks);
        updateState = LevelUpdateState.None;
    }

    public void Tick(TimeSpan time) {
        updateState = LevelUpdateState.Tick;
        // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
        foreach(TChunk chunk in _chunks.Values) {
            if(chunk.ticks <= 0)
                continue;
            chunk.Tick(time);
            chunk.ticks--;
        }
        foreach(TChunk chunk in _chunks.Values)
            chunk.PopulateDirty(_dirtyObjects);
        // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
        foreach(TObject obj in _dirtyObjects)
            CheckDirty(obj);
        _dirtyObjects.Clear();
        AddNewChunks();
        foreach(Vector2Int pos in _relightQueue)
            GetChunkAt(pos).ClearLighting();
        foreach(Vector2Int pos in _relightQueue)
            GetChunkAt(pos).UpdateLighting();
        _relightQueue.Clear();
        updateState = LevelUpdateState.None;
    }

    public void CheckDirty(TObject obj) {
        if(obj.positionDirty) {
            ILight? light = obj as ILight;
            Vector2Int fromChunkPos = LevelToChunkPosition(obj.internalPrevPosition);
            Vector2Int toChunkPos = LevelToChunkPosition(obj.position);
            if(fromChunkPos != toChunkPos) {
                GetChunkAt(fromChunkPos).Remove(obj);
                GetChunkAt(toChunkPos).Add(obj);
                if(light is not null)
                    TryQueueLight(obj.internalPrevPosition, light);
            }
            if(light is not null)
                TryQueueLight(obj.position, light);
            // ReSharper disable once SuspiciousTypeConversion.Global
            if(obj is IMovable movable)
                movable.Moved(obj.internalPrevPosition);
            obj.positionDirty = false;
        }
        if(!obj.dirty)
            return;
        objectChanged?.Invoke(obj);
        obj.ClearDirty();
    }

    private void UpdateChunksInBounds(TimeSpan time, Bounds bounds) {
        for(int x = bounds.min.x; x != bounds.max.x + 1; x++) {
            if(x > _maxChunkPos.x)
                x = _minChunkPos.x;
            for(int y = bounds.min.y; y != bounds.max.y + 1; y++) {
                if(y > _maxChunkPos.y)
                    y = _minChunkPos.y;
                TChunk chunk = GetChunkAt(new Vector2Int(x, y));
                chunk.ticks++;
                chunk.Update(time);
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
                Vector2Int pos = new(x, y);
                GetChunkAt(pos).Draw(ChunkToScreenPosition(pos));
            }
        }
    }

    private void AddNewChunks() {
        foreach((Vector2Int pos, TChunk chunk) in _autoChunks) {
            if(chunk.ticks <= 0)
                continue;
            _chunks.Add(pos, chunk);
            _chunksToGenerate.Add((ChunkToLevelPosition(pos), pos));
        }
        foreach((Vector2Int levelPosition, Vector2Int pos) in _chunksToGenerate) {
            _autoChunks.Remove(pos);
            // weird chunk size, skip gen
            if(LevelToChunkPosition(levelPosition + chunkSize - new Vector2Int(1, 1)) != pos)
                continue;
            chunkCreated?.Invoke(levelPosition, chunkSize);
        }
        _chunksToGenerate.Clear();
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

    public void LoadChunkAt(Vector2Int chunkPosition) => GetChunkAt(chunkPosition).ticks += 2;
    internal TChunk GetChunkAt(Vector2Int chunkPosition) {
        if(_chunks.TryGetValue(chunkPosition, out TChunk? chunk) ||
            _autoChunks.TryGetValue(chunkPosition, out chunk))
            return chunk;
        chunk = new TChunk();
        chunk.SetLevel(this);
        chunk.InitLighting();
        _autoChunks.Add(chunkPosition, chunk);
        return chunk;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public Vector2Int LevelToCameraPosition(Vector2Int levelPosition) => levelPosition - cameraPosition;
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public Vector2Int LevelToScreenPosition(Vector2Int levelPosition) =>
        CameraToScreenPosition(LevelToCameraPosition(levelPosition));
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public Vector2Int LevelToChunkPosition(Vector2Int levelPosition) =>
        new(MoreMath.FloorDiv(levelPosition.x, chunkSize.x), MoreMath.FloorDiv(levelPosition.y, chunkSize.y));
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public Vector2Int LevelToInChunkPosition(Vector2Int levelPosition) =>
        new(MoreMath.Mod(levelPosition.x, chunkSize.x), MoreMath.Mod(levelPosition.y, chunkSize.y));

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public Vector2Int CameraToLevelPosition(Vector2Int cameraPosition) => cameraPosition + this.cameraPosition;
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public Vector2Int CameraToScreenPosition(Vector2Int cameraPosition) => cameraPosition + renderer.size / 2;
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public Vector2Int CameraToChunkPosition(Vector2Int cameraPosition) =>
        LevelToChunkPosition(CameraToLevelPosition(cameraPosition));

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public Vector2Int ScreenToLevelPosition(Vector2Int screenPosition) =>
        ScreenToCameraPosition(CameraToLevelPosition(screenPosition));
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public Vector2Int ScreenToCameraPosition(Vector2Int screenPosition) => screenPosition - renderer.size / 2;
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public Vector2Int ScreenToChunkPosition(Vector2Int screenPosition) =>
        LevelToChunkPosition(ScreenToLevelPosition(screenPosition));

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public Vector2Int ChunkToLevelPosition(Vector2Int chunkPosition) =>
        new(chunkPosition.x * chunkSize.x, chunkPosition.y * chunkSize.y);
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public Vector2Int ChunkToCameraPosition(Vector2Int chunkPosition) =>
        LevelToCameraPosition(ChunkToLevelPosition(chunkPosition));
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
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
