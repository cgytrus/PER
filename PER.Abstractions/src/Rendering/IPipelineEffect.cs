using System.Collections.Generic;
using System.Numerics;

using JetBrains.Annotations;

using PER.Util;

namespace PER.Abstractions.Rendering;

[PublicAPI]
public interface IPipelineEffect : IEffect {
    public IEnumerable<PipelineStep>? pipeline { get; }
}
