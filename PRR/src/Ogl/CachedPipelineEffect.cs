using System.Linq;

using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;

using PER.Abstractions.Rendering;
using PER.Util;

namespace PRR.Ogl;

public class CachedPipelineEffect {
    public CachedPipelineEffect(Vector2i viewSize, Vector2i imageSize, IPipelineEffect? effect) {
        pipeline = effect?.pipeline?.Select(step => {
            Shader? shader = step.vertexShader is null || step.fragmentShader is null ? null :
                new Shader(step.vertexShader, step.fragmentShader);
            if(shader is not null)
                SetUniforms(shader, viewSize, imageSize);
            CachedPipelineStep cachedStep = new() {
                type = step.stepType,
                shader = shader,
                blendMode = Converters.ToPrrBlendMode(step.blendMode)
            };
            return cachedStep;
        }).ToArray();
    }

    private static void SetUniforms(Shader shader, Vector2i viewSize, Vector2i imageSize) {
        shader.Use();
        int viewSizeL = shader.GetUniformLocation("viewSize");
        int imageSizeL = shader.GetUniformLocation("imageSize");
        int font = shader.GetUniformLocation("font");
        int target = shader.GetUniformLocation("target");
        int current = shader.GetUniformLocation("current");
        if(viewSizeL != -1)
            GL.Uniform2(viewSizeL, viewSize);
        if(imageSizeL != -1)
            GL.Uniform2(imageSizeL, imageSize);
        if(font != -1)
            GL.Uniform1(font, 0);
        if(target != -1)
            GL.Uniform1(target, 1);
        if(current != -1)
            GL.Uniform1(current, 2);
    }

    public CachedPipelineStep[]? pipeline { get; private set; }
}
