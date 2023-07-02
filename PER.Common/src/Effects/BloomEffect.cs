using JetBrains.Annotations;

using PER.Abstractions.Rendering;
using PER.Abstractions.Resources;

namespace PER.Common.Effects;

[PublicAPI]
public class BloomEffect : Resource, IPipelineEffect {
    public const string GlobalId = "graphics/effects/bloom";

    public IEnumerable<PipelineStep>? pipeline { get; private set; }

    public override void Preload() {
        AddPath("vertex", "graphics/shaders/default_vert.glsl");
        AddPath("fragment", "graphics/shaders/bloom_frag.glsl");
        AddPath("blend", "graphics/shaders/bloom-blend_frag.glsl");
    }

    public override void Load(string id) {
        string vertexPath = GetPath("vertex");
        string fragmentPath = GetPath("fragment");
        string blendPath = GetPath("blend");

        pipeline = new[] {
            new PipelineStep {
                stepType = PipelineStep.Type.TemporaryText,
                blendMode = BlendMode.alpha
            },
            new PipelineStep {
                stepType = PipelineStep.Type.SwapBuffer
            },
            new PipelineStep {
                stepType = PipelineStep.Type.TemporaryScreen,
                vertexShader = File.ReadAllText(vertexPath),
                fragmentShader = File.ReadAllText(fragmentPath),
                blendMode = BlendMode.alpha
            },
            new PipelineStep {
                stepType = PipelineStep.Type.SwapBuffer
            },
            new PipelineStep {
                stepType = PipelineStep.Type.TemporaryScreen,
                vertexShader = File.ReadAllText(vertexPath),
                fragmentShader = File.ReadAllText(fragmentPath),
                blendMode = BlendMode.alpha
            },
            new PipelineStep {
                stepType = PipelineStep.Type.SwapBuffer
            },
            new PipelineStep {
                stepType = PipelineStep.Type.ClearBuffer
            },
            new PipelineStep {
                stepType = PipelineStep.Type.TemporaryText,
                blendMode = BlendMode.alpha
            },
            new PipelineStep {
                stepType = PipelineStep.Type.Screen,
                vertexShader = File.ReadAllText(vertexPath),
                fragmentShader = File.ReadAllText(blendPath),
                blendMode = BlendMode.alpha
            }
        };
    }

    public override void Unload(string id) => pipeline = null;
}
