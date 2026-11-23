using Silk.NET.OpenGL;

namespace GLUI.rendering.opengl.attributes;

public class IntAttribute : Attribute {
	public IntAttribute(GL gl) : base(gl, VertexAttribPointerType.Int) { }
}
