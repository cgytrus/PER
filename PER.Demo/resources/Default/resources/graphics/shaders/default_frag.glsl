#version 330 core

uniform int step;
uniform sampler2D font;
uniform sampler2D target;
uniform sampler2D current;

in vec2 texCoord;
in vec4 backgroundColor;
in vec4 foregroundColor;

out vec4 fragColor;

void main() {
    vec4 top = foregroundColor * texture(font, texCoord.st);
    float t = (1f - top.a) * backgroundColor.a;
    float a = t + top.a;
    vec3 final = (t * backgroundColor.rgb + top.a * top.rgb) / a;
    fragColor = vec4(final, a);
}
