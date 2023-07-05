using System.Collections.Generic;

using PER.Util;

namespace PER.Abstractions.Environment;

public static class LightPropagator {
    private static bool _swapProp;
    private static readonly HashSet<Vector2Int> prop0 = new();
    private static readonly HashSet<Vector2Int> prop1 = new();
    private static readonly HashSet<Vector2Int> visited = new();
    private static HashSet<Vector2Int> prop => _swapProp ? prop1 : prop0;
    private static HashSet<Vector2Int> otherProp => _swapProp ? prop0 : prop1;

    public static void UpdateLitByLights<TLevel, TChunk, TObject>(Level<TLevel, TChunk, TObject> level,
        Chunk<TLevel, TChunk, TObject> chunk)
        where TLevel : Level<TLevel, TChunk, TObject>
        where TChunk : Chunk<TLevel, TChunk, TObject>, new()
        where TObject : LevelObject<TLevel, TChunk, TObject> {
        if(!level.doLighting)
            return;
        for(int i = chunk.litBy.Count - 1; i >= 0; i--) {
            ILight? light = chunk.litBy[i];
            if(light is not TObject { inLevelInt: true } lightObj)
                continue;
            ResetLight(level, lightObj);
            PropagateLight(level, lightObj);
        }
        chunk.litBy.RemoveAll(x => x is null);
    }

    public static void UpdateBlockedLights<TLevel, TChunk, TObject>(Level<TLevel, TChunk, TObject> level,
        LevelObject<TLevel, TChunk, TObject> obj)
        where TLevel : Level<TLevel, TChunk, TObject>
        where TChunk : Chunk<TLevel, TChunk, TObject>, new()
        where TObject : LevelObject<TLevel, TChunk, TObject> {
        if(!level.doLighting || obj.blockedLights is null || !obj.blocksLight)
            return;
        for(int i = obj.blockedLights.Count - 1; i >= 0; i--) {
            ILight? light = obj.blockedLights[i];
            if(light is not TObject { inLevelInt: true } lightObj)
                continue;
            ResetLight(level, lightObj);
            PropagateLight(level, lightObj);
        }
        obj.blockedLights.RemoveAll(x => x is null);
    }

    public static void ResetLight<TLevel, TChunk, TObject>(Level<TLevel, TChunk, TObject> level,
        LevelObject<TLevel, TChunk, TObject> obj)
        where TLevel : Level<TLevel, TChunk, TObject>
        where TChunk : Chunk<TLevel, TChunk, TObject>, new()
        where TObject : LevelObject<TLevel, TChunk, TObject> {
        if(!level.doLighting || obj.contributedLight is null || obj is not ILight light)
            return;
        foreach((Vector2Int pos, (float lighting, int visibility)) in obj.contributedLight) {
            Vector2Int inChunk = level.LevelToInChunkPosition(pos);
            TChunk chunk = level.GetChunkAt(level.LevelToChunkPosition(pos));

            chunk.lighting[inChunk.y, inChunk.x] -= lighting;
            chunk.visibility[inChunk.y, inChunk.x] -= visibility;

            int index = chunk.litBy.IndexOf(light);
            if(index >= 0)
                chunk.litBy[index] = null;
            chunk.MarkNotBlockingLightAt(pos, light);
        }
        obj.contributedLight.Clear();
    }

    public static void PropagateLight<TLevel, TChunk, TObject>(Level<TLevel, TChunk, TObject> level,
        LevelObject<TLevel, TChunk, TObject> obj)
        where TLevel : Level<TLevel, TChunk, TObject>
        where TChunk : Chunk<TLevel, TChunk, TObject>, new()
        where TObject : LevelObject<TLevel, TChunk, TObject> {
        if(!level.doLighting || obj is not ILight light)
            return;

        byte emission = light.emission;
        byte visibility = light.visibility;

        if(emission == 0 && visibility == 0)
            return;

        prop.Add(obj.position);
        while(emission > 0 || visibility > 0) {
            float lighting = emission * light.brightness / light.emission;
            foreach(Vector2Int pos in prop) {
                visited.Add(pos);

                Vector2Int inChunk = level.LevelToInChunkPosition(pos);
                TChunk chunk = level.GetChunkAt(level.LevelToChunkPosition(pos));

                chunk.lighting[inChunk.y, inChunk.x] += lighting;
                chunk.visibility[inChunk.y, inChunk.x] += visibility;

                chunk.litBy.Add(light);
                obj.contributedLight?.Add(pos, (lighting, visibility));
                if(chunk.TryMarkBlockingLightAt(pos, light))
                    continue;

                Vector2Int offset = pos - obj.position;

                if(offset.x <= 0 && !visited.Contains(pos + new Vector2Int(-1, 0)))
                    otherProp.Add(pos + new Vector2Int(-1, 0));
                if(offset.x >= 0 && !visited.Contains(pos + new Vector2Int(1, 0)))
                    otherProp.Add(pos + new Vector2Int(1, 0));
                if(offset.y <= 0 && !visited.Contains(pos + new Vector2Int(0, -1)))
                    otherProp.Add(pos + new Vector2Int(0, -1));
                if(offset.y >= 0 && !visited.Contains(pos + new Vector2Int(0, 1)))
                    otherProp.Add(pos + new Vector2Int(0, 1));
            }
            prop.Clear();
            _swapProp = !_swapProp;
            if(emission > 0)
                emission--;
            if(visibility > 0)
                visibility--;
        }

        _swapProp = false;
        prop0.Clear();
        prop1.Clear();
        visited.Clear();
    }
}
