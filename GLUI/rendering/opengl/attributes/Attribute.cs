using Silk.NET.OpenGL;

namespace GLUI.rendering.opengl.attributes;

public class Attribute {
	private GL gl;
	private VertexAttribPointerType dataType;
	private bool normalized;
	private int componentCount;
	private uint bytesPerVertex;

	public Attribute(GL gl, VertexAttribPointerType dataType, int componentCount = 1, bool normalized = false) {
		this.gl = gl;
		this.dataType = dataType;
		this.normalized = normalized;
		this.componentCount = componentCount;
		bytesPerVertex = calculateBytes();
	}

	public void enable(uint location) => gl.EnableVertexAttribArray(location);
	public void disable(uint location) => gl.DisableVertexAttribArray(location);

	public unsafe void link(uint location, uint offset, uint stride) {
		if (dataType is VertexAttribPointerType.Int or VertexAttribPointerType.UnsignedInt) 
			gl.VertexAttribIPointer(location, componentCount, VertexAttribIType.Int, stride, (void*) offset);
		else gl.VertexAttribPointer(location, componentCount, dataType, normalized, stride, (void*) offset);
	}

	public uint getSize() => bytesPerVertex;
	public int getComponentCount() => componentCount;
	
	private uint calculateBytes() =>
		dataType switch {
			VertexAttribPointerType.Float => (uint)(4 * componentCount),
			VertexAttribPointerType.UnsignedInt => (uint)(4 * componentCount),
			VertexAttribPointerType.Int => (uint)(4 * componentCount),
			VertexAttribPointerType.Short => (uint)(2 * componentCount),
			VertexAttribPointerType.UnsignedShort => (uint)(2 * componentCount),
			VertexAttribPointerType.Byte => (uint)(1 * componentCount),
			VertexAttribPointerType.UnsignedByte => (uint)(1 * componentCount),
			VertexAttribPointerType.UnsignedInt2101010Rev => 4u,
			_ => throw new ArgumentException("Unsupported data type for VAO attribute: " + dataType)
		};
}
