using System;

using PER.Abstractions.Environment;
using PER.Abstractions.Rendering;
using PER.Util;

namespace PER.Demo.Environment;

public class EffectObject : LevelObject {
    public Vector2Int size { get; set; }
    public IEffect? effect { get; set; }

    protected override RenderCharacter character { get; } = new('a', Color.transparent, Color.white);
    public override void Update(TimeSpan time) { }
    public override void Tick(TimeSpan time) { }
    public override void Draw() {
        for(int y = 0; y < size.y; y++)
            for(int x = 0; x < size.x; x++)
                renderer.AddEffect(level.LevelToScreenPosition(position) + new Vector2Int(x, y), effect);
    }
}
