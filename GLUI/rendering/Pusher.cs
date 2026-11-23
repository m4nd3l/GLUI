using GLUI.ui.utils;

namespace GLUI.rendering;

public partial class Renderer {
	public void beginBatch() {
		vertices ??= [];
		indices ??= [];
		vertices.Clear();
		indices.Clear();
	}

	public void endBatch() => uploadGPU();
	
	public void drawQuad(float x = 200.0f, float y = 200.0f, float width = 100.0f, float height = 100.0f, 
		Color? filler = null, Color? border = null, int textureSlot = -1, float borderThickness = 0f, float progress = 100f, 
		float cornerRadius = 25f, bool startLeft = true) {
		vertices ??= [];
		indices ??= [];
		filler ??= Color.white;
		border ??= Color.blue;

		var baseVertex = (uint) (vertices.Count / vao.getVertexComponents());
		
		addVertex(x, y,                  0f, 0f, width, height, filler, border, textureSlot,  0f, borderThickness, progress, cornerRadius, startLeft);
		addVertex(x + width, y,          1f, 0f, width, height, filler, border, textureSlot,  0f, borderThickness, progress, cornerRadius, startLeft);
		addVertex(x + width, y + height, 1f, 1f, width, height, filler, border, textureSlot,  0f, borderThickness, progress, cornerRadius, startLeft);
		addVertex(x, y + height,         0f, 1f, width, height, filler, border, textureSlot,  0f, borderThickness, progress, cornerRadius, startLeft);

		indices.Add(baseVertex + 0);
		indices.Add(baseVertex + 1);
		indices.Add(baseVertex + 2);
		indices.Add(baseVertex + 2);
		indices.Add(baseVertex + 3);
		indices.Add(baseVertex + 0);
	}
	
	private void addVertex(float x, float y, float u, float v, float width, float height, Color filler, Color border, int textureSlot, float shapeType, float borderThickness, float progress, float cornerRadius, bool startLeft) {
		vertices ??= new List<float>((int) vao.getVertexComponents());

		vertices.EnsureCapacity(vertices.Count + (int) vao.getVertexComponents());
		
		vertices.Add(x); vertices.Add(y);
		vertices.Add(u); vertices.Add(v);
		vertices.Add(width); vertices.Add(height);
		vertices.Add(filler.r()); vertices.Add(filler.g()); vertices.Add(filler.b()); vertices.Add(filler.a());
		vertices.Add(border.r()); vertices.Add(border.g()); vertices.Add(border.b()); vertices.Add(border.a());
		vertices.Add(BitConverter.Int32BitsToSingle(textureSlot));
		vertices.Add(shapeType); vertices.Add(borderThickness); vertices.Add(progress); vertices.Add(cornerRadius);
		vertices.Add(BitConverter.Int32BitsToSingle(startLeft ? 1 : 0));
	}
}
