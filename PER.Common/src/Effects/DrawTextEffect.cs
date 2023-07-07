using JetBrains.Annotations;

using PER.Abstractions.Rendering;
using PER.Abstractions.Resources;

namespace PER.Common.Effects;

[PublicAPI]
public class DrawTextEffect : Resource, IPipelineEffect {
    public const string GlobalId = "graphics/effects/text";

    public IEnumerable<PipelineStep>? pipeline { get; private set; }

    public override void Preload() {
        AddPath("vertex", "graphics/shaders/default_vert.glsl");
        AddPath("fragment", "graphics/shaders/default_frag.glsl");
    }

    public override void Load(string id) {
        string vertexPath = GetPath("vertex");
        string fragmentPath = GetPath("fragment");

        pipeline = new[] {
            new PipelineStep {
                stepType = PipelineStep.Type.Text,
                vertexShader = File.ReadAllText(vertexPath),
                fragmentShader = File.ReadAllText(fragmentPath),
                blendMode = BlendMode.alpha
            }
        };
    }

    public override void Unload(string id) => pipeline = null;
}
