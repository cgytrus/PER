using System.Collections.Generic;

using PER.Util;

namespace PER.Abstractions.Environment;

public class LightPropagator<TLevel, TChunk, TObject>
    where TLevel : Level<TLevel, TChunk, TObject>
    where TChunk : Chunk<TLevel, TChunk, TObject>, new()
    where TObject : LevelObject<TLevel, TChunk, TObject> {
    private readonly Level<TLevel, TChunk, TObject> _level;

    private bool _swapProp;
    private readonly HashSet<Vector2Int> _prop0 = new();
    private readonly HashSet<Vector2Int> _prop1 = new();
    private readonly HashSet<Vector2Int> _visited = new();
    private HashSet<Vector2Int> prop => _swapProp ? _prop1 : _prop0;
    private HashSet<Vector2Int> otherProp => _swapProp ? _prop0 : _prop1;

    private readonly HashSet<LevelObject<TLevel, TChunk, TObject>> _lightsToReset = new();
    private readonly HashSet<LevelObject<TLevel, TChunk, TObject>> _lightsToPropagate = new();

    public LightPropagator(Level<TLevel, TChunk, TObject> level) => _level = level;

    public void QueueLitBy(Chunk<TLevel, TChunk, TObject> chunk) {
        if(!_level.doLighting)
            return;
        for(int i = chunk.litBy.Count - 1; i >= 0; i--) {
            if(chunk.litBy[i] is not TObject { inLevelInt: true } lightObj)
                continue;
            QueueReset(lightObj);
            QueuePropagate(lightObj);
        }
        chunk.litBy.RemoveAll(x => x is null);
    }

    public void QueueBlockedBy(LevelObject<TLevel, TChunk, TObject> obj) {
        if(!_level.doLighting || obj.blockedLights is null || !obj.blocksLight)
            return;
        for(int i = obj.blockedLights.Count - 1; i >= 0; i--) {
            if(obj.blockedLights[i] is not TObject { inLevelInt: true } lightObj)
                continue;
            QueueReset(lightObj);
            QueuePropagate(lightObj);
        }
        obj.blockedLights.RemoveAll(x => x is null);
    }

    public void QueueReset(LevelObject<TLevel, TChunk, TObject> obj) {
        if(_level.doLighting && obj.contributedLight is not null && obj is ILight)
            _lightsToReset.Add(obj);
    }

    public void QueuePropagate(LevelObject<TLevel, TChunk, TObject> obj) {
        if(_level.doLighting && obj.contributedLight is not null && obj is ILight)
            _lightsToPropagate.Add(obj);
    }

    public void UpdateQueued() {
        if(!_level.doLighting)
            return;
        foreach(LevelObject<TLevel, TChunk, TObject> obj in _lightsToReset)
            Reset(obj);
        foreach(LevelObject<TLevel, TChunk, TObject> obj in _lightsToPropagate)
            Propagate(obj);
        _lightsToReset.Clear();
        _lightsToPropagate.Clear();
    }

    private void Reset(LevelObject<TLevel, TChunk, TObject> obj) {
        // silencing null checks as everything in here should already be checked before by
        // QueueResetLight, QueuePropagateLight and UpdateQueued
        ILight light = (obj as ILight)!;
        foreach((Vector2Int pos, Color lighting) in obj.contributedLight!) {
            Vector2Int inChunk = _level.LevelToInChunkPosition(pos);
            TChunk chunk = _level.GetChunkAt(_level.LevelToChunkPosition(pos));

            chunk.lighting[inChunk.y, inChunk.x] -= lighting;
            chunk.totalVisibility -= lighting.a;

            int index = chunk.litBy.IndexOf(light);
            if(index >= 0)
                chunk.litBy[index] = null;
            chunk.MarkNotBlockingLightAt(pos, light);
        }
        obj.contributedLight.Clear();
    }

    private void Propagate(LevelObject<TLevel, TChunk, TObject> obj) {
        ILight light = (obj as ILight)!;

        byte emission = light.emission;
        byte reveal = light.reveal;

        if(emission == 0 && reveal == 0)
            return;

        prop.Add(obj.position);
        while(emission > 0 || reveal > 0) {
            Color3 rgb = emission == 0 ? Color3.black : emission * light.color / light.emission;
            float a = reveal == 0 ? 0f : reveal / (float)light.reveal;
            Color lighting = new(rgb, a);
            foreach(Vector2Int pos in prop)
                PropagateStep(obj, pos, lighting);
            prop.Clear();
            _swapProp = !_swapProp;
            if(emission > 0)
                emission--;
            if(reveal > 0)
                reveal--;
        }

        _swapProp = false;
        _prop0.Clear();
        _prop1.Clear();
        _visited.Clear();
    }

    private void PropagateStep(LevelObject<TLevel, TChunk, TObject> obj, Vector2Int pos, Color lighting) {
        ILight light = (obj as ILight)!;

        _visited.Add(pos);

        Vector2Int inChunk = _level.LevelToInChunkPosition(pos);
        TChunk chunk = _level.GetChunkAt(_level.LevelToChunkPosition(pos));

        chunk.lighting[inChunk.y, inChunk.x] += lighting;
        chunk.totalVisibility += lighting.a;

        chunk.litBy.Add(light);
        obj.contributedLight!.Add(pos, lighting);
        if(chunk.TryMarkBlockingLightAt(pos, light))
            return;

        Vector2Int offset = pos - obj.position;

        if(offset.x <= 0 && !_visited.Contains(pos + new Vector2Int(-1, 0)))
            otherProp.Add(pos + new Vector2Int(-1, 0));
        if(offset.x >= 0 && !_visited.Contains(pos + new Vector2Int(1, 0)))
            otherProp.Add(pos + new Vector2Int(1, 0));
        if(offset.y <= 0 && !_visited.Contains(pos + new Vector2Int(0, -1)))
            otherProp.Add(pos + new Vector2Int(0, -1));
        if(offset.y >= 0 && !_visited.Contains(pos + new Vector2Int(0, 1)))
            otherProp.Add(pos + new Vector2Int(0, 1));
    }
}
