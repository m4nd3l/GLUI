#version 330 core

in vec2 fLocalUV;
in vec2 fTextureUV;
in vec2 fSize;
in vec4 fFill;
in vec4 fBorder;
in vec4 fShapeArgs;
flat in int fTextureSlot;
flat in int fDirection;

out vec4 fragColor;

// Types: 0 = rounded rectangle, 1 = ellipse, 2 = ring,
//        3 = polygon triangle (CPU triangulated), 4 = line capsule.
// fShapeArgs = [type, border/line/ring thickness px, progress% (or line length px), radius%]
uniform sampler2D uTextures[16];

const float PI = 3.14159265358979323846;
const float TWO_PI = 6.28318530717958647692;

// Explicit constant sampler indices are compatible with GLSL 330.
vec4 readTexture(int slot, vec2 uv) {
    switch (slot) {
        case  0: return texture(uTextures[ 0], uv);
        case  1: return texture(uTextures[ 1], uv);
        case  2: return texture(uTextures[ 2], uv);
        case  3: return texture(uTextures[ 3], uv);
        case  4: return texture(uTextures[ 4], uv);
        case  5: return texture(uTextures[ 5], uv);
        case  6: return texture(uTextures[ 6], uv);
        case  7: return texture(uTextures[ 7], uv);
        case  8: return texture(uTextures[ 8], uv);
        case  9: return texture(uTextures[ 9], uv);
        case 10: return texture(uTextures[10], uv);
        case 11: return texture(uTextures[11], uv);
        case 12: return texture(uTextures[12], uv);
        case 13: return texture(uTextures[13], uv);
        case 14: return texture(uTextures[14], uv);
        case 15: return texture(uTextures[15], uv);
        default: return vec4(1.0);
    }
}

float coverage(float signedDistance) {
    float antialias = max(fwidth(signedDistance), 0.001);
    return 1.0 - smoothstep(-antialias, antialias, signedDistance);
}

float sdRoundedRect(vec2 p, vec2 halfSize, float r) {
    vec2 d = abs(p) - halfSize + vec2(r);
    return length(max(d, 0.0)) + min(max(d.x, d.y), 0.0) - r;
}

// Approximate pixel-distance to an ellipse (exact for circles).
float sdEllipse(vec2 p, vec2 radii) {
    vec2 safeRadii = max(radii, vec2(0.001));
    return (length(p / safeRadii) - 1.0) * min(safeRadii.x, safeRadii.y);
}

// Untextured track/border, textured and tinted foreground.
vec4 roundedRectangle(vec2 p, vec2 size, float thickness, float progress, float radius, vec4 fill) {
    vec2 halfSize = size * 0.5;
    float outerMask = coverage(sdRoundedRect(p, halfSize, radius));
    vec2 insideSize = max(size - vec2(thickness * 2.0), 0.0);

    float fillWidth = min(progress * size.x, insideSize.x);
    float foregroundMask = 0.0;
    if (fillWidth > 0.0 && insideSize.y > 0.0) {
        vec2 fillHalf = vec2(fillWidth, insideSize.y) * 0.5;
        float centerX = (fDirection != 0)
            ? -halfSize.x + thickness + fillHalf.x
            :  halfSize.x - thickness - fillHalf.x;
        float fillRadius = min(radius, min(fillHalf.x, fillHalf.y));
        foregroundMask = coverage(sdRoundedRect(p - vec2(centerX, 0.0), fillHalf, fillRadius));
    }

    vec4 color = mix(fBorder, fill, foregroundMask);
    color.a *= outerMask;
    return color;
}

vec4 ellipse(vec2 p, vec2 size, float thickness, vec4 fill) {
    vec2 outerRadii = size * 0.5;
    float outerMask = coverage(sdEllipse(p, outerRadii));
    if (thickness <= 0.0) return vec4(fill.rgb, fill.a * outerMask);

    vec2 innerRadii = outerRadii - vec2(thickness);
    float innerMask = 0.0;
    if (min(innerRadii.x, innerRadii.y) > 0.0)
        innerMask = coverage(sdEllipse(p, innerRadii));

    vec4 color = mix(fBorder, fill, innerMask);
    color.a *= outerMask;
    return color;
}

// Starts at 12 o'clock; direction=1 clockwise on screen, 0 counterclockwise.
vec4 ring(vec2 p, vec2 size, float thickness, float progress, vec4 fill) {
    if (progress <= 0.0 || thickness <= 0.0) return vec4(0.0);

    float outerRadius = min(size.x, size.y) * 0.5;
    thickness = min(thickness, outerRadius * 2.0);
    float pathRadius = max(outerRadius - thickness * 0.5, 0.0);
    float halfThickness = thickness * 0.5;
    float d = abs(length(p) - pathRadius) - halfThickness;

    if (progress < 1.0) {
        float sweep = progress * TWO_PI;
        float direction = fDirection != 0 ? 1.0 : -1.0;
        float angle = mod(atan(direction * p.x, -p.y) + TWO_PI, TWO_PI);
        if (angle > sweep) {
            vec2 startPoint = vec2(0.0, -pathRadius);
            vec2 endPoint = vec2(direction * sin(sweep), -cos(sweep)) * pathRadius;
            d = min(length(p - startPoint), length(p - endPoint)) - halfThickness;
        }
    }

    return vec4(fill.rgb, fill.a * coverage(d));
}

vec4 lineCapsule(vec2 p, float lengthPx, float thickness, vec4 fill) {
    vec2 closest = vec2(clamp(p.x, -lengthPx * 0.5, lengthPx * 0.5), 0.0);
    float d = length(p - closest) - thickness * 0.5;
    return vec4(fill.rgb, fill.a * coverage(d));
}

void main() {
    int type = int(fShapeArgs.x + 0.5);
    float minSide = min(fSize.x, fSize.y);
    float thickness = clamp(fShapeArgs.y, 0.0, minSide * 0.5);
    float progress = clamp(fShapeArgs.z / 100.0, 0.0, 1.0);
    float radius = clamp(fShapeArgs.w * 0.01 * minSide, 0.0, minSide * 0.5);

    vec2 p = (fLocalUV - vec2(0.5)) * fSize;
    // Texture UVs are independent of local SDF UVs, supporting atlas regions.
    vec4 fill = fFill * readTexture(fTextureSlot, fTextureUV);

    if (type == 0) {
        fragColor = roundedRectangle(p, fSize, thickness, progress, radius, fill);
    } else if (type == 1) {
        fragColor = ellipse(p, fSize, thickness, fill);
    } else if (type == 2) {
        // Ring thickness can be as large as the diameter, unlike a border.
        float ringThickness = clamp(fShapeArgs.y, 0.0, minSide);
        fragColor = ring(p, fSize, ringThickness, progress, fill);
    } else if (type == 3) {
        fragColor = fill; // The polygon is CPU triangulated.
    } else if (type == 4) {
        fragColor = lineCapsule(p, max(fShapeArgs.z, 0.0), max(fShapeArgs.y, 0.0), fill);
    } else {
        fragColor = vec4(0.0);
    }
}
