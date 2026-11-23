using Silk.NET.OpenGL;

namespace GLUI.rendering.opengl.attributes;

public class Vec4fAttribute : Attribute {
	public Vec4fAttribute(GL gl) : base(gl, VertexAttribPointerType.Float, 4) { }
}
