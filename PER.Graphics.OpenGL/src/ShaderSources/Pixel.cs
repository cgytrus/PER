namespace PER.Graphics.OpenGL.ShaderSources;

internal static class Pixel {
    public const string Vertex = """
        #version 330 core

        uniform isampler2D offsetTex;

        layout(location = 0) in ivec2 aPosition;
        layout(location = 1) in vec4 aBackground;
        layout(location = 2) in vec4 aForeground;
        layout(location = 3) in int aCharacter;
        layout(location = 4) in ivec3 aStyle;
        layout(location = 5) in ivec2 aOffset;

        out vec4 background;
        out vec4 foreground;
        flat out int character;
        flat out ivec3 style;
        flat out ivec2 offset;

        void main() {
            gl_Position = vec4(vec2(aPosition.x + 0.5, aPosition.y + 0.5) / textureSize(offsetTex, 0) * 2.0 - vec2(1.0), 0.0, 1.0);
            background = aBackground;
            foreground = aForeground;
            character = aCharacter;
            style = aStyle;
            offset = aOffset;
        }
        """;

    public const string Fragment = """
        #version 330 core

        uniform sampler2D backgroundTex;
        uniform sampler2D foregroundTex;
        uniform sampler2D characterTex;
        uniform isampler2D styleTex;
        uniform isampler2D offsetTex;

        in vec4 background;
        in vec4 foreground;
        flat in int character;
        flat in ivec3 style;
        flat in ivec2 offset;

        layout(location = 0) out vec4 bg;
        layout(location = 1) out vec4 fg;
        layout(location = 2) out vec4 ch;
        layout(location = 3) out ivec3 st;
        layout(location = 4) out ivec2 off;

        void main() {
            bg = background;
            fg = foreground;
            ch = character < 0 ? vec4(0.0) : vec4(character, 0.0, 0.0, 1.0);
            st = style;
            off = offset;
        }
        """;
}
