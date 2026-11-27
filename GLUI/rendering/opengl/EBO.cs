using Silk.NET.OpenGL;

namespace GLUI.rendering.opengl;

public class EBO : RenderObject {
	private uint ebo { get; }
	
	public EBO(GL gl) : base(gl) => ebo = gl.GenBuffer();

	public unsafe void updateData(uint[] indices, BufferUsageARB drawType = BufferUsageARB.StaticDraw) {
		fixed (uint* buf = indices)
			gl.BufferData(BufferTargetARB.ElementArrayBuffer, (nuint) (indices.Length * sizeof(uint)), buf, drawType);
	}
	
	public sealed override void bind() => gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, ebo);
	public sealed override void unbind() => gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, 0);
	public sealed override void disposeImplementation() { unbind(); gl.DeleteBuffer(ebo); }
}
