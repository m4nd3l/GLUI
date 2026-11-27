using Silk.NET.OpenGL;

namespace GLUI.rendering.opengl.uniforms;

public unsafe class Uniform1 : Uniform {
	public Uniform1(GL gl, string name, uint shaderProgram, int* value) : base(gl, name, shaderProgram, value) { }

	public override void upload() {
		int current;
		try { current = *(int*) value; }
		catch { throw new MissingFieldException("void* value in Matrix4Uniform is not pointing to int structure"); }
		
		gl.Uniform1(location, current);
	}
}
