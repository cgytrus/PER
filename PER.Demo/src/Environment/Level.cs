using System;

using PER.Abstractions.Audio;
using PER.Abstractions.Environment;
using PER.Abstractions.Input;
using PER.Abstractions.Rendering;
using PER.Abstractions.Resources;
using PER.Util;

namespace PER.Demo.Environment;

public class Level : Level<Level, Chunk, LevelObject> {
    public Level(IRenderer renderer, IInput input, IAudio audio, IResources resources, Vector2Int chunkSize) : base(
        renderer, input, audio, resources, chunkSize) { }
    protected override TimeSpan maxGenerationTime => Core.engine.tickInterval;
    protected override void GenerateChunk(Vector2Int start) { }
}
