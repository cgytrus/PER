using JetBrains.Annotations;

using PER.Abstractions.Rendering;

namespace PER.Common.Effects;

[PublicAPI]
public class DrawTextEffect : IPipelineEffect {
    public IEnumerable<PipelineStep>? pipeline { get; } = new[] {
        new PipelineStep {
            stepType = PipelineStep.Type.Text,
            blendMode = BlendMode.alpha
        }
    };
}
