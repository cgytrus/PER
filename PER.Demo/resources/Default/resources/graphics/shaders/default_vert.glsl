#version 330 core

uniform ivec2 viewSize;
uniform ivec2 imageSize;

layout(location = 0) in vec2 aPosition;
layout(location = 1) in vec2 aTexCoord;
layout(location = 2) in vec4 aBackgroundColor;
layout(location = 3) in vec4 aForegroundColor;

out vec2 texCoord;
out vec4 backgroundColor;
out vec4 foregroundColor;

void main() {
    gl_Position = vec4((aPosition * vec2(2.0, -2.0) - vec2(imageSize.x, -imageSize.y)) / viewSize, 0.0, 1.0);
    texCoord = aTexCoord;
    backgroundColor = aBackgroundColor;
    foregroundColor = aForegroundColor;
}
