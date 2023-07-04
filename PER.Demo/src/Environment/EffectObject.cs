using PER.Abstractions.Rendering;
using PER.Util;

namespace PER.Demo.Environment;

public class EffectObject : LevelObject {
    public IDisplayEffect? useEffect { get; init; }

    public override int layer => 2;
    public override RenderCharacter character => renderer.GetCharacter(position);
    public override IDisplayEffect? effect => useEffect;
    public override bool blocksLight => false;
}
