#version 330 core

layout(location = 0) in vec2 aPosition;
layout(location = 1) in vec2 aLocalUV;
layout(location = 2) in vec2 aTextureUV;
layout(location = 3) in vec2 aSize;
layout(location = 4) in vec4 aFill;
layout(location = 5) in vec4 aBorder;
layout(location = 6) in int aTextureSlot;
layout(location = 7) in vec4 aShapeArgs;
layout(location = 8) in int aDirection;

uniform mat4 uProjection;

out vec2 fLocalUV;
out vec2 fTextureUV;
out vec2 fSize;
out vec4 fFill;
out vec4 fBorder;
out vec4 fShapeArgs;
flat out int fTextureSlot;
flat out int fDirection;

void main() {
    fLocalUV = aLocalUV;
    fTextureUV = aTextureUV;
    fSize = aSize;
    fFill = aFill;
    fBorder = aBorder;
    fShapeArgs = aShapeArgs;
    fTextureSlot = aTextureSlot;
    fDirection = aDirection;
    gl_Position = uProjection * vec4(aPosition, 0.0, 1.0);
}
