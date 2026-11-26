using GLUI.ui.utils;
using System.Numerics;
using Texture = GLUI.rendering.opengl.Texture;

namespace GLUI.rendering;

public unsafe partial class GLUIRenderer {
    private const float epsilon = 0.00001f;
    private static readonly Vector4 fullUV = new(0f, 0f, 1f, 1f);

    private enum ShapeType { RoundedRectangle = 0, Ellipse = 1, Ring = 2, Polygon = 3, Line = 4 }
    
    public void drawQuad(
        float x = 200f, float y = 200f, float width = 100f, float height = 100f,
        Color? filler = null, Color? border = null, int textureSlot = -1,
        float borderThickness = 0f, float progress = 100f,
        float cornerRadius = 12f, bool startLeft = true,
        Texture? texture = null, Vector4? uvRect = null) {

        throwIfDisposed();
        validateBounds(width, height);
        if (width == 0f || height == 0f) return;

        int slot = resolveTextureSlot(texture, textureSlot);
        emitQuad(
            new Vector2(x, y), new Vector2(x + width, y),
            new Vector2(x + width, y + height), new Vector2(x, y + height),
            new Vector2(width, height), filler ?? Color.white, border ?? Color.blue,
            slot, ShapeType.RoundedRectangle, Math.Max(borderThickness, 0f), progress,
            cornerRadius, startLeft, uvRect ?? fullUV);
    }

    public void drawRectangle(float x, float y, float width, float height,
        Color? fill = null, Texture? texture = null, Vector4? uvRect = null)
        => drawQuad(x, y, width, height, fill, Color.transparent, -1, 0f, 100f, 0f, true, texture, uvRect);

    public void drawRoundedRectangle(float x, float y, float width, float height,
        float radiusPercent = 12f, Color? fill = null, Color? border = null,
        float borderThickness = 0f, Texture? texture = null, Vector4? uvRect = null)
            => drawQuad(x, y, width, height, fill, border ?? Color.transparent, -1,
                borderThickness, 100f, radiusPercent, true, texture, uvRect);
    
    public void drawCircle(float centerX, float centerY, float radius,
        Color? fill = null, Color? border = null, float borderThickness = 0f,
        Texture? texture = null, Vector4? uvRect = null) 
            => drawEllipse(centerX - radius, centerY - radius, radius * 2f, radius * 2f,
                fill, border, borderThickness, texture, uvRect);
    
    public void drawEllipse(float x, float y, float width, float height,
        Color? fill = null, Color? border = null, float borderThickness = 0f,
        Texture? texture = null, Vector4? uvRect = null) {

        throwIfDisposed();
        validateBounds(width, height);
        if (width == 0f || height == 0f) return;
        int slot = resolveTextureSlot(texture);
        emitQuad(
            new Vector2(x, y), new Vector2(x + width, y),
            new Vector2(x + width, y + height), new Vector2(x, y + height),
            new Vector2(width, height), fill ?? Color.white, border ?? Color.transparent,
            slot, ShapeType.Ellipse, Math.Max(borderThickness, 0f), 100f, 0f, true, uvRect ?? fullUV);
    }

    // Ring/arc start at 12 o'clock. progress is 0..100% of a revolution.
    public void drawRing(float centerX, float centerY, float outerRadius, float thickness,
        Color? color = null, float progress = 100f, bool clockwise = true,
        Texture? texture = null, Vector4? uvRect = null) {

        throwIfDisposed();
        if (outerRadius < 0f) throw new ArgumentOutOfRangeException(nameof(outerRadius));
        if (thickness < 0f) throw new ArgumentOutOfRangeException(nameof(thickness));
        if (outerRadius == 0f || thickness == 0f || progress <= 0f) return;
        float diameter = outerRadius * 2f;
        int slot = resolveTextureSlot(texture);
        emitQuad(
            new Vector2(centerX - outerRadius, centerY - outerRadius),
            new Vector2(centerX + outerRadius, centerY - outerRadius),
            new Vector2(centerX + outerRadius, centerY + outerRadius),
            new Vector2(centerX - outerRadius, centerY + outerRadius),
            new Vector2(diameter), color ?? Color.white, Color.transparent,
            slot, ShapeType.Ring, thickness, progress, 0f, clockwise, uvRect ?? fullUV);
    }

    public void drawArc(float centerX, float centerY, float radius, float thickness,
        float sweepPercent, Color? color = null, bool clockwise = true,
        Texture? texture = null, Vector4? uvRect = null) =>
        drawRing(centerX, centerY, radius, thickness, color, sweepPercent, clockwise, texture, uvRect);

    // A line is a shader-antialiased capsule, so ends are rounded.
    public void drawLine(Vector2 start, Vector2 end, float thickness = 2f,
        Color? color = null, Texture? texture = null, Vector4? uvRect = null) {

        throwIfDisposed();
        if (thickness < 0f) throw new ArgumentOutOfRangeException(nameof(thickness));
        if (thickness == 0f) return;
        Vector2 axis = end - start;
        float length = axis.Length();
        if (length < epsilon) {
            drawCircle(start.X, start.Y, thickness * 0.5f, color, texture: texture, uvRect: uvRect);
            return;
        }

        Vector2 tangent = axis / length;
        Vector2 normal = new(-tangent.Y, tangent.X);
        float pad = thickness * 0.5f + 1.5f;
        Vector2 left = start - tangent * pad;
        Vector2 right = end + tangent * pad;
        Vector2 v = normal * pad;

        int slot = resolveTextureSlot(texture);
        emitQuad(
            left - v, right - v, right + v, left + v,
            new Vector2(length + 2f * pad, 2f * pad), color ?? Color.white,
            Color.transparent, slot, ShapeType.Line, thickness, length, 0f, true,
            uvRect ?? fullUV);
    }

    public void drawTriangle(Vector2 a, Vector2 b, Vector2 c,
        Color? fill = null, Color? border = null, float borderThickness = 0f,
        Texture? texture = null, Vector4? uvRect = null) =>
        drawPolygon(new[] { a, b, c }, fill, border, borderThickness, texture, uvRect);

    // Supports simple convex AND concave polygons (without holes).
    // Fill is ear-clipped on the CPU. Border is drawn as antialiased capsules
    // centered on each edge, so translucent borders may overlap at corners.
    public void drawPolygon(IReadOnlyList<Vector2> points,
        Color? fill = null, Color? border = null, float borderThickness = 0f,
        Texture? texture = null, Vector4? uvRect = null) {

        throwIfDisposed();
        ArgumentNullException.ThrowIfNull(points);
        if (borderThickness < 0f) throw new ArgumentOutOfRangeException(nameof(borderThickness));
        List<Vector2> polygon = normalizePolygon(points);
        if (polygon.Count < 3) throw new ArgumentException("A polygon needs at least 3 distinct vertices.", nameof(points));

        List<(int a, int b, int c)> triangles = triangulate(polygon);

        float minX = float.MaxValue, minY = float.MaxValue;
        float maxX = float.MinValue, maxY = float.MinValue;
        foreach (Vector2 p in polygon) {
            minX = Math.Min(minX, p.X); minY = Math.Min(minY, p.Y);
            maxX = Math.Max(maxX, p.X); maxY = Math.Max(maxY, p.Y);
        }

        Vector2 size = new(maxX - minX, maxY - minY);
        if (size.X <= epsilon || size.Y <= epsilon)
            throw new ArgumentException("Polygon area must be nonzero.", nameof(points));

        int slot = resolveTextureSlot(texture);
        Vector4 atlas = uvRect ?? fullUV;
        uint baseVertex = (uint)(vertices.Count / vertexWords);
        foreach (Vector2 p in polygon) {
            float u = (p.X - minX) / size.X;
            float v = (p.Y - minY) / size.Y;
            appendVertex(p, new Vector2(u, v), atlas, size,
                fill ?? Color.white, Color.transparent, slot, ShapeType.Polygon,
                0f, 100f, 0f, true);
        }
        foreach (var (a, b, c) in triangles) {
            indices.Add(baseVertex + (uint)a);
            indices.Add(baseVertex + (uint)b);
            indices.Add(baseVertex + (uint)c);
        }

        if (borderThickness > 0f && border is not null && border.a() > 0f) {
            for (int i = 0; i < polygon.Count; i++)
                drawLine(polygon[i], polygon[(i + 1) % polygon.Count], borderThickness, border);
        }
    }

    public void drawRegularPolygon(float centerX, float centerY, float radius, int sides,
        Color? fill = null, Color? border = null, float borderThickness = 0f,
        float rotationDegrees = -90f, Texture? texture = null, Vector4? uvRect = null) {

        if (sides < 3) throw new ArgumentOutOfRangeException(nameof(sides), "At least 3 sides are required.");
        if (radius <= 0f) throw new ArgumentOutOfRangeException(nameof(radius));
        Vector2[] points = new Vector2[sides];
        float rotation = rotationDegrees * (MathF.PI / 180f);
        for (int i = 0; i < sides; i++) {
            float angle = rotation + i * (2f * MathF.PI / sides);
            points[i] = new Vector2(centerX + MathF.Cos(angle) * radius,
                                    centerY + MathF.Sin(angle) * radius);
        }
        drawPolygon(points, fill, border, borderThickness, texture, uvRect);
    }

    // Internal common geometry path. No GL calls occur here.
    private void emitQuad(Vector2 topLeft, Vector2 topRight, Vector2 bottomRight, Vector2 bottomLeft,
        Vector2 size, Color fill, Color border, int slot, ShapeType type,
        float thickness, float progressOrLength, float radiusPercent, bool direction, Vector4 uvRect) {

        uint first = (uint)(vertices.Count / vertexWords);
        appendVertex(topLeft,     new Vector2(0f, 0f), uvRect, size, fill, border, slot, type, thickness, progressOrLength, radiusPercent, direction);
        appendVertex(topRight,    new Vector2(1f, 0f), uvRect, size, fill, border, slot, type, thickness, progressOrLength, radiusPercent, direction);
        appendVertex(bottomRight, new Vector2(1f, 1f), uvRect, size, fill, border, slot, type, thickness, progressOrLength, radiusPercent, direction);
        appendVertex(bottomLeft,  new Vector2(0f, 1f), uvRect, size, fill, border, slot, type, thickness, progressOrLength, radiusPercent, direction);
        indices.Add(first); indices.Add(first + 1); indices.Add(first + 2);
        indices.Add(first + 2); indices.Add(first + 3); indices.Add(first);
    }

    private void appendVertex(Vector2 p, Vector2 localUV, Vector4 uvRect, Vector2 size,
        Color fill, Color border, int slot, ShapeType type,
        float thickness, float progressOrLength, float radiusPercent, bool direction) {

        float textureU = uvRect.X + (uvRect.Z - uvRect.X) * localUV.X;
        float textureV = uvRect.Y + (uvRect.W - uvRect.Y) * localUV.Y;

        vertices.Add(p.X); vertices.Add(p.Y);
        vertices.Add(localUV.X); vertices.Add(localUV.Y);
        vertices.Add(textureU); vertices.Add(textureV);
        vertices.Add(size.X); vertices.Add(size.Y);
        vertices.Add(fill.r()); vertices.Add(fill.g()); vertices.Add(fill.b()); vertices.Add(fill.a());
        vertices.Add(border.r()); vertices.Add(border.g()); vertices.Add(border.b()); vertices.Add(border.a());
        vertices.Add(BitConverter.Int32BitsToSingle(slot));
        vertices.Add((float)type); vertices.Add(thickness); vertices.Add(progressOrLength); vertices.Add(radiusPercent);
        vertices.Add(BitConverter.Int32BitsToSingle(direction ? 1 : 0));
    }

    private static void validateBounds(float width, float height) {
        if (!float.IsFinite(width) || !float.IsFinite(height) || width < 0f || height < 0f)
            throw new ArgumentOutOfRangeException(nameof(width), "Width and height must be finite and nonnegative.");
    }

    private static float cross(Vector2 a, Vector2 b, Vector2 c) =>
        (b.X - a.X) * (c.Y - a.Y) - (b.Y - a.Y) * (c.X - a.X);

    private static List<Vector2> normalizePolygon(IReadOnlyList<Vector2> source) {
        List<Vector2> result = [];
        foreach (Vector2 p in source) {
            if (!float.IsFinite(p.X) || !float.IsFinite(p.Y))
                throw new ArgumentException("Polygon vertices must be finite.", nameof(source));
            if (result.Count == 0 || Vector2.DistanceSquared(result[^1], p) > epsilon * epsilon)
                result.Add(p);
        }
        if (result.Count >= 2 && Vector2.DistanceSquared(result[0], result[^1]) <= epsilon * epsilon)
            result.RemoveAt(result.Count - 1);

        // Remove redundant, collinear vertices (without changing the outline).
        bool changed;
        do {
            changed = false;
            if (result.Count <= 3) break;
            for (int i = 0; i < result.Count; i++) {
                Vector2 a = result[(i - 1 + result.Count) % result.Count];
                Vector2 b = result[i];
                Vector2 c = result[(i + 1) % result.Count];
                if (MathF.Abs(cross(a, b, c)) <= epsilon && Vector2.Dot(b - a, b - c) <= 0f) {
                    result.RemoveAt(i);
                    changed = true;
                    break;
                }
            }
        } while (changed);
        return result;
    }

    private static List<(int a, int b, int c)> triangulate(IReadOnlyList<Vector2> p) {
        float doubledArea = 0f;
        for (int i = 0; i < p.Count; i++) {
            Vector2 a = p[i], b = p[(i + 1) % p.Count];
            doubledArea += a.X * b.Y - b.X * a.Y;
        }
        if (MathF.Abs(doubledArea) <= epsilon)
            throw new ArgumentException("Polygon has zero signed area.", nameof(p));
        float sign = MathF.Sign(doubledArea);
        List<int> remaining = Enumerable.Range(0, p.Count).ToList();
        List<(int, int, int)> output = [];

        while (remaining.Count > 3) {
            bool foundEar = false;
            for (int i = 0; i < remaining.Count; i++) {
                int prev = remaining[(i - 1 + remaining.Count) % remaining.Count];
                int curr = remaining[i];
                int next = remaining[(i + 1) % remaining.Count];
                if (sign * cross(p[prev], p[curr], p[next]) <= epsilon) continue;

                bool containsVertex = false;
                foreach (int vertex in remaining) {
                    if (vertex == prev || vertex == curr || vertex == next) continue;
                    if (sign * cross(p[prev], p[curr], p[vertex]) >= -epsilon &&
                        sign * cross(p[curr], p[next], p[vertex]) >= -epsilon &&
                        sign * cross(p[next], p[prev], p[vertex]) >= -epsilon) {
                        containsVertex = true;
                        break;
                    }
                }
                if (containsVertex) continue;
                output.Add((prev, curr, next));
                remaining.RemoveAt(i);
                foundEar = true;
                break;
            }
            if (!foundEar)
                throw new ArgumentException("Cannot triangulate polygon: check for self-intersections or degeneracy.", nameof(p));
        }
        output.Add((remaining[0], remaining[1], remaining[2]));
        return output;
    }
}
