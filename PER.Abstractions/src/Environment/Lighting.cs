using System.Collections.Generic;

using PER.Util;

namespace PER.Abstractions.Environment;

public abstract class Lighting<TLevel, TChunk, TObject>(Level<TLevel, TChunk, TObject> level)
    where TLevel : Level<TLevel, TChunk, TObject>
    where TChunk : Chunk<TLevel, TChunk, TObject>, new()
    where TObject : LevelObject<TLevel, TChunk, TObject> {
    private readonly HashSet<LevelObject<TLevel, TChunk, TObject>> _lightsToReset = [];
    private readonly HashSet<LevelObject<TLevel, TChunk, TObject>> _lightsToPropagate = [];

    public void QueueLitBy(Chunk<TLevel, TChunk, TObject> chunk) {
        if(!level.doLighting)
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
        if(!level.doLighting || obj.blockedLights is null || !obj.blocksLight)
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
        if(level.doLighting && obj.contributedLight is not null && obj is ILight)
            _lightsToReset.Add(obj);
    }

    public void QueuePropagate(LevelObject<TLevel, TChunk, TObject> obj) {
        if(level.doLighting && obj.contributedLight is not null && obj is ILight)
            _lightsToPropagate.Add(obj);
    }

    public void UpdateQueued() {
        if(!level.doLighting)
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
            Vector2Int inChunk = level.LevelToInChunkPosition(pos);
            TChunk chunk = level.GetChunkAt(level.LevelToChunkPosition(pos));

            chunk.lighting[inChunk.y, inChunk.x] -= lighting;
            chunk.totalVisibility -= lighting.a;

            int index = chunk.litBy.IndexOf(light);
            if(index >= 0)
                chunk.litBy[index] = null;
            chunk.MarkNotBlockingLightAt(pos, light);
        }
        obj.contributedLight.Clear();
    }

    protected abstract void Propagate(LevelObject<TLevel, TChunk, TObject> obj);
}
