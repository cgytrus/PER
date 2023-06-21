using System;

using PER.Abstractions.Environment;
using PER.Abstractions.Rendering;
using PER.Util;

namespace PER.Demo.Environment;

public class WallObject : LevelObject {
    protected override RenderCharacter character { get; } = new('#', Color.transparent, Color.white);
    public override void Update(TimeSpan time) { }
    public override void Tick(TimeSpan time) { }
}
