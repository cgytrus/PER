﻿using PER.Abstractions.Rendering;

namespace PRR.OpenGL;

public struct CachedPipelineStep {
    public PipelineStep.Type type { get; init; }
    public Shader? shader { get; init; }
    public BlendMode blendMode { get; init; }
}