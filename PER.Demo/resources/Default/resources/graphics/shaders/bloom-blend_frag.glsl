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
    vec4 targetTex = texture(target, texCoord.st);
    vec4 currentTex = texture(current, texCoord.st);

    vec4 result = vec4(max(targetTex.x, currentTex.x),
                       max(targetTex.y, currentTex.y),
                       max(targetTex.z, currentTex.z),
                       max(targetTex.w, currentTex.w));

    fragColor = result;
}
