using System;
using System.Collections.Generic;
using System.Numerics;

using JetBrains.Annotations;

using PER.Util;

namespace PER.Abstractions.Environment;

[PublicAPI]
public class PathTracedLighting<TLevel, TChunk, TObject>(Level<TLevel, TChunk, TObject> level, byte maxIndirect) :
    Lighting<TLevel, TChunk, TObject>(level)
    where TLevel : Level<TLevel, TChunk, TObject>
    where TChunk : Chunk<TLevel, TChunk, TObject>, new()
    where TObject : LevelObject<TLevel, TChunk, TObject> {
    private readonly Level<TLevel, TChunk, TObject> _level = level;

    private readonly HashSet<Vector2Int> _visited = [];

    private readonly record struct Source(Vector2Int pos, ILight light, Color3 color, float emission, float reveal,
        Dictionary<Vector2Int, Color> contributedLight);

    protected override void Propagate(LevelObject<TLevel, TChunk, TObject> obj) {
        ILight light = (obj as ILight)!;
        if(light is { emission: 0, reveal: 0 })
            return;
        Propagate(new Source(obj.position, light, light.color, light.emission, light.reveal, obj.contributedLight!), 0);
        _visited.Clear();
    }

    private void Propagate(Source source, byte depth) {
        if(source is { emission: <= 0f, reveal: <= 0f })
            return;
        // rays == circumference of a circle with the radius of our light source
        int rays = (int)MathF.Ceiling(MathF.Tau * Math.Max(source.emission, source.reveal));
        if(rays == 0)
            return;
        _visited.Add(source.pos);
        float da = MathF.Tau / rays;
        for(int i = 0; i < rays; i++) {
            (float x, float y) = MathF.SinCos(da * i);
            Cast(source, new Vector2(x, y), depth);
        }
    }

    private void Cast(Source source, Vector2 dir, byte depth) {
        int x = 0;
        int dx = Math.Sign(dir.X);
        float dtX = dir.X > 0 ? 1f / dir.X : 0f;
        float ddtX = dx / dir.X;

        int y = 0;
        int dy = Math.Sign(dir.Y);
        float dtY = dir.Y > 0 ? 1f / dir.Y : 0f;
        float ddtY = dy / dir.Y;

        Vector2Int prev = source.pos;
        float r = Math.Max(source.emission, source.reveal);
        while(x * x + y * y <= r * r) {
            Vector2Int pos = new(source.pos.x + x, source.pos.y + y);
            if(!Draw(source, pos, prev, depth))
                break;
            prev = pos;
            if(dtX < dtY) {
                x += dx;
                dtX += ddtX;
            }
            else {
                y += dy;
                dtY += ddtY;
            }
        }
    }

    private bool Draw(Source source, Vector2Int pos, Vector2Int prev, byte depth) {
        Vector2Int inChunk = _level.LevelToInChunkPosition(pos);
        TChunk chunk = _level.GetChunkAt(_level.LevelToChunkPosition(pos));

        float dist = new Vector2(pos.x - source.pos.x, pos.y - source.pos.y).Length();
        Color3 rgb = dist >= source.emission ? Color3.black : source.color * (1f - dist / source.emission);
        float a = dist >= source.reveal ? 0f : 1f - dist / source.reveal;
        Color lighting = new(rgb, a);

        if(source.contributedLight.TryGetValue(pos, out Color prevLighting)) {
            lighting = new Color(Math.Max(lighting.r, prevLighting.r), Math.Max(lighting.g, prevLighting.g),
                Math.Max(lighting.b, prevLighting.b), Math.Max(lighting.a, prevLighting.a));
            chunk.lighting[inChunk.y, inChunk.x] -= prevLighting;
            chunk.totalVisibility -= prevLighting.a;
            source.contributedLight[pos] = lighting;
        }
        else {
            chunk.litBy.Add(source.light);
            source.contributedLight.Add(pos, lighting);
        }

        chunk.lighting[inChunk.y, inChunk.x] += lighting;
        chunk.totalVisibility += lighting.a;

        if(!chunk.TryMarkBlockingLightAt(pos, source.light, out Color hitCol))
            return true;
        if(depth + 1 > maxIndirect || _visited.Contains(prev))
            return false;
        Propagate(new Source(prev, source.light, rgb * new Color3(hitCol.r, hitCol.g, hitCol.b),
            Math.Max(source.emission - dist, 0f), 0f, source.contributedLight), (byte)(depth + 1));
        return false;
    }
}
