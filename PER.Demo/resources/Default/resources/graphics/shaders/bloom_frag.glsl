#version 330 core

uniform int step;
uniform sampler2D font;
uniform sampler2D target;
uniform sampler2D current;

in vec2 texCoord;
in vec4 backgroundColor;
in vec4 foregroundColor;

out vec4 fragColor;

float weight[5] = float[](0.2270270270, 0.1945945946, 0.1216216216, 0.0540540541, 0.0162162162);

void main() {
    vec2 texOffset = 1.0 / textureSize(current, 0); // gets size of single texel
    vec4 result = texture(current, texCoord.st) * weight[0];

    if(step == 2) {
        for(int i = 1; i < 5; ++i) {
            result += texture(current, texCoord.st + vec2(texOffset.x * i, 0.0)) * weight[i];
            result += texture(current, texCoord.st - vec2(texOffset.x * i, 0.0)) * weight[i];
        }
    }
    else {
        for(int i = 1; i < 5; ++i) {
            result += texture(current, texCoord.st + vec2(0.0, texOffset.y * i)) * weight[i];
            result += texture(current, texCoord.st - vec2(0.0, texOffset.y * i)) * weight[i];
        }
    }

    fragColor = result;
}
