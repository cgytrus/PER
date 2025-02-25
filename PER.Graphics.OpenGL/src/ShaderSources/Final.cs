namespace PER.Graphics.OpenGL.ShaderSources;

internal static class Final {
    public const string Vertex = """
        #version 330 core

        uniform ivec2 charSize; // font size
        uniform vec2 normCharSize; // normalized to viewport

        uniform isampler2D styleTex;
        uniform isampler2D offsetTex;

        layout(location = 0) in vec2 aPosition;

        flat out ivec2 position;
        out vec2 texCoord;

        void main() {
            ivec2 size = textureSize(styleTex, 0);
            position = ivec2(gl_InstanceID % size.x, gl_InstanceID / size.x);
        
            int italic = texelFetch(styleTex, position, 0).z;
            ivec2 offset = texelFetch(offsetTex, position, 0).xy;
        
            vec2 processedPos = aPosition + position + (vec2(italic * (1.0 - aPosition.y), 0.0) + offset) / charSize;
        
            gl_Position = vec4((processedPos * vec2(2.0, -2.0) - vec2(size.x, -size.y)) * normCharSize, 0.0, 1.0);
            texCoord = aPosition * charSize;
        }
        """;

    public const string Fragment = """
        #version 330 core

        uniform sampler2D backgroundTex;
        uniform sampler2D foregroundTex;
        uniform sampler2D characterTex;
        uniform isampler2D styleTex;

        uniform sampler2D font;
        uniform sampler2D formatting;
        uniform isamplerBuffer charMap;

        flat in ivec2 position;
        in vec2 texCoord;

        out vec4 color;

        vec4 blend(vec4 bottom, vec4 top) {
            float t = (1.0 - top.a) * bottom.a;
            float a = t + top.a;
            return vec4((t * bottom.rgb + top.a * top.rgb) / a, a);
        }

        void main() {
            vec4 backgroundColor = texelFetch(backgroundTex, position, 0);
            vec4 foregroundColor = texelFetch(foregroundTex, position, 0);
            ivec2 character = ivec2(texelFetch(charMap, int(texelFetch(characterTex, position, 0).x)).xy + texCoord);
            ivec3 style = texelFetch(styleTex, position, 0).xyz;
            int bold = style.x;
            int underlineStrikethrough = style.y;
        
            vec4 foreground = texelFetch(font, character, 0);
            foreground = max(foreground, bold * texelFetch(font, character - ivec2(1, 0), 0));
            foreground = max(foreground, texelFetch(formatting, ivec2(underlineStrikethrough, int(texCoord.y)), 0));
        
            color = blend(backgroundColor, foregroundColor * foreground);
        }
        """;
}
