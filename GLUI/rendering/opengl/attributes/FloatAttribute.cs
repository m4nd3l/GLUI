using Silk.NET.OpenGL;

namespace GLUI.rendering.opengl.attributes;

public class FloatAttribute : Attribute {
	public FloatAttribute(GL gl) : base(gl, VertexAttribPointerType.Float) { }
}
