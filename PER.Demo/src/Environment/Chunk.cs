using PER.Abstractions.Environment;

namespace PER.Demo.Environment;

public class Chunk : Chunk<Level, Chunk, LevelObject> {
    protected override bool shouldUpdate => true;
    protected override bool shouldTick => true;
}
