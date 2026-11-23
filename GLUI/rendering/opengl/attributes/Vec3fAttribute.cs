using Silk.NET.OpenGL;

namespace GLUI.rendering.opengl.attributes;

public class Vec3fAttribute : Attribute {
	public Vec3fAttribute(GL gl) : base(gl, VertexAttribPointerType.Float, 3) { }
}
