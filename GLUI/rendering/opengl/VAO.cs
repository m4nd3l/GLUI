using Silk.NET.OpenGL;
using Attribute = GLUI.rendering.opengl.attributes.Attribute;

namespace GLUI.rendering.opengl;

public class VAO : RenderObject {
	private uint vao { get; }
	private List<Attribute> attributes { get; } = [];

	public VAO(GL gl) : base(gl) => vao = gl.GenVertexArray();

	public void addAttributes(params Attribute[] newAttributes) {
		bind();

		foreach (Attribute attribute in newAttributes)
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

	public uint getVertexComponents()
		=> (uint) attributes.Sum(attribute => attribute.getComponentCount());
	
	private uint getVertexSize() 
		=> attributes.Aggregate<Attribute?, uint>(0, (current, attribute) => current + (attribute?.getSize() ?? 0));
	

	public sealed override void bind() => gl.BindVertexArray(vao);
	public sealed override void unbind() => gl.BindVertexArray(0);
	public sealed override void disposeImplementation() { unbind(); gl.DeleteVertexArray(vao); }
}
