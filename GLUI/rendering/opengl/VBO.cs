using Silk.NET.OpenGL;

namespace GLUI.rendering.opengl;

public class VBO : RenderObject {
	private uint vbo { get; }

	public VBO(GL gl) : base(gl) => vbo = gl.GenBuffer();

	public unsafe void updateData(float[] vertices, BufferUsageARB drawType = BufferUsageARB.StaticDraw) {
		fixed (float* buf = vertices)
			gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint) (vertices.Length * sizeof(float)), buf, drawType);
	}
	
	public sealed override void bind() => gl.BindBuffer(BufferTargetARB.ArrayBuffer, vbo);
	public sealed override void unbind() => gl.BindBuffer(BufferTargetARB.ArrayBuffer, 0);
	public sealed override void disposeImplementation() { unbind(); gl.DeleteBuffer(vbo); }
}
