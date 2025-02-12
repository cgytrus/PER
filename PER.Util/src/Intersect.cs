using System;
using System.Numerics;
using JetBrains.Annotations;

namespace PER.Util;

[PublicAPI]
public static class Intersect {
    // https://stackoverflow.com/a/100165/10484146
    public static bool RectSegment(Bounds r, Vector2Int p0, Vector2Int p1) {
        // Find min and max X for the segment
        int minX = Math.Min(p0.x, p1.x);
        int maxX = Math.Max(p0.x, p1.x);

        // Find the intersection of the segment's and rectangle's X-projections
        if (maxX > r.max.x)
            maxX = r.max.x;
        if (minX < r.min.x)
            minX = r.min.x;

        // If their projections do not intersect return false
        if (minX > maxX)
            return false;

        // Find corresponding min and max Y for min and max X we found before
        int minY = p0.y;
        int maxY = p1.y;

        if (p0.x != p1.x) {
            float dx = p1.x - p0.x;
            float dy = p1.y - p0.y;
            float a = dy / dx;
            float b = p0.y - a * p0.x;
            minY = (int)(a * minX + b);
            maxY = (int)(a * maxX + b);
        }

        if (minY > maxY)
            (maxY, minY) = (minY, maxY);

        if (maxY > r.max.y)
            maxY = r.max.y;
        if (minY < r.min.y)
            minY = r.min.y;

        // If Y-projections do not intersect return false
        if (minY > maxY)
            return false;

        return true;
    }

    public static bool RectSegmentOut(Bounds r, Vector2Int p0, Vector2Int p1, out float d) {
        d = 1f;
        if (r.Contains(p1))
            return true;
        if (p0 == p1)
            return false;

        int px0 = Math.Min(p0.x, p1.x);
        int py0 = Math.Min(p0.y, p1.y);
        int px1 = Math.Max(p0.x, p1.x);
        int py1 = Math.Max(p0.y, p1.y);

        int rx0 = r.min.x;
        int ry0 = r.min.y;
        int rx1 = r.max.x;
        int ry1 = r.max.y;

        int rx = p0.x > p1.x ? rx0 : rx1;
        if (px0 <= rx && rx <= px1) {
            d = (float)(rx - p0.x) / (px1 - px0);
            float y = MathF.FusedMultiplyAdd(p1.y - p0.y, d, p0.y);
            if (ry0 <= y && y <= ry1)
                return true;
        }

        int ry = p0.y > p1.y ? ry0 : ry1;
        if (py0 <= ry && ry <= py1) {
            d = (float)(ry - p0.y) / (py1 - py0);
            float x = MathF.FusedMultiplyAdd(p1.x - p0.x, d, p0.x);
            if (rx0 <= x && x <= rx1)
                return true;
        }

        return false;
    }
}
