using Silk.NET.OpenGL;
using Attribute = GLUI.rendering.opengl.attributes.Attribute;

namespace GLUI.rendering.opengl;

public class VAO : RenderObject {
	private uint vao;
	private List<Attribute> attributes;

	public VAO(GL glInstance) : base(glInstance) {
		vao = gl().GenVertexArray();
		attributes = new();
	}

	public void addAttributes(params Attribute[] newAttributes) {
		bind();

		foreach (var attribute in newAttributes)
			attributes.Add(attribute);

		uint stride = getVertexSize();
		uint offset = 0;

		for (uint i = 0; i < attributes.Count; i++) {
			Attribute attribute = attributes[(int) i];
			attribute.enable(i);
			attribute.link(i, offset, stride);
			offset += attribute.getSize();
		}
	}

	public uint getVertexComponents() {
		int vertexComponents = 0;
		foreach (Attribute attribute in attributes) vertexComponents += attribute.getComponentCount();
		return (uint) vertexComponents;
	}
	
	private uint getVertexSize() {
		uint size = 0;
		foreach (var attribute in attributes) size += attribute.getSize();
		return size;
	}

	public sealed override void bind() => gl().BindVertexArray(vao);
	public sealed override void unbind() => gl().BindVertexArray(0);
	public sealed override void disposeImplementation() { unbind(); gl().DeleteVertexArray(vao); }
}
