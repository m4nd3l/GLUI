using Silk.NET.OpenGL;

namespace GLUI.rendering.opengl.attributes;

public class Vec2fAttribute : Attribute {
	public Vec2fAttribute(GL gl) : base(gl, VertexAttribPointerType.Float, 2) { }
}
