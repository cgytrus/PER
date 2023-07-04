using PER.Abstractions.Rendering;
using PER.Util;

namespace PER.Demo.Environment;

public class FloorObject : LevelObject {
    public override int layer => -1;
    public override RenderCharacter character { get; } =
        new('.', Color.transparent, new Color(0.1f, 0.1f, 0.1f, 1f));
    public override bool blocksLight => false;
}
