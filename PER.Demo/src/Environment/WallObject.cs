using PER.Abstractions.Rendering;
using PER.Util;

namespace PER.Demo.Environment;

public class WallObject : LevelObject {
    public override int layer => 1;
    public override RenderCharacter character { get; } = new('#', Color.transparent, Color.white);
    public override bool blocksLight => false;
}
