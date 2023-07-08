using OpenTK.Graphics.OpenGL;

namespace PRR.OpenGL;

public readonly record struct BlendMode(
    BlendingFactorSrc colorSrcFactor, BlendingFactorDest colorDstFactor, BlendEquationMode colorEquation,
    BlendingFactorSrc alphaSrcFactor, BlendingFactorDest alphaDstFactor, BlendEquationMode alphaEquation
    ) {
    public void Use() {
        GL.BlendFuncSeparate(colorSrcFactor, colorDstFactor, alphaSrcFactor, alphaDstFactor);
        GL.BlendEquationSeparate(colorEquation, alphaEquation);
    }
}
