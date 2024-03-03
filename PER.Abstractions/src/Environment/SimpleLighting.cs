using System.Collections.Generic;

using JetBrains.Annotations;

using PER.Util;

namespace PER.Abstractions.Environment;

[PublicAPI]
public class SimpleLighting<TLevel, TChunk, TObject> : Lighting<TLevel, TChunk, TObject>
    where TLevel : Level<TLevel, TChunk, TObject>
    where TChunk : Chunk<TLevel, TChunk, TObject>, new()
    where TObject : LevelObject<TLevel, TChunk, TObject> {
    private bool _swapProp;
    private readonly HashSet<Vector2Int> _prop0 = [];
    private readonly HashSet<Vector2Int> _prop1 = [];
    private readonly HashSet<Vector2Int> _visited = [];
    private HashSet<Vector2Int> prop => _swapProp ? _prop1 : _prop0;
    private HashSet<Vector2Int> otherProp => _swapProp ? _prop0 : _prop1;

    protected override void Propagate(LevelObject<TLevel, TChunk, TObject> obj) {
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

        Vector2Int inChunk = level.LevelToInChunkPosition(pos);
        TChunk chunk = level.GetChunkAt(level.LevelToChunkPosition(pos));

        chunk.lighting[inChunk.y, inChunk.x] += lighting;
        chunk.totalVisibility += lighting.a;

        chunk.litBy.Add(light);
        obj.contributedLight!.Add(pos, lighting);
        if(chunk.TryMarkBlockingLightAt(pos, light, out _))
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
