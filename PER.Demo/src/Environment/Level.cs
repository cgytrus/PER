using System;

using PER.Abstractions.Audio;
using PER.Abstractions.Environment;
using PER.Abstractions.Input;
using PER.Abstractions.Rendering;
using PER.Abstractions.Resources;
using PER.Util;

namespace PER.Demo.Environment;

public class Level(Vector2Int chunkSize) : Level<Level, Chunk, LevelObject>(true, chunkSize) {
    protected override TimeSpan maxGenerationTime => Engine.tickInterval;
    protected override void GenerateChunk(Vector2Int start) { }
}
