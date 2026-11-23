using Silk.NET.Maths;
using Silk.NET.OpenGL;

namespace GLUI.rendering.opengl.uniforms;

public unsafe class Matrix4fUniform : Uniform {
	public Matrix4fUniform(GL gl, string name, uint shaderProgram, Matrix4X4<float>* value) : base(gl, name, shaderProgram, value) { }

	public override void upload() {
		Matrix4X4<float> current;
		try { current = *(Matrix4X4<float>*) getValue(); }
		catch { throw new MissingFieldException("void* value in Matrix4Uniform is not pointing to Matrix4X4<float> structure"); }
		
		float* data = stackalloc float[16] {
			current.M11, current.M12, current.M13, current.M14,
			current.M21, current.M22, current.M23, current.M24,
			current.M31, current.M32, current.M33, current.M34,
			current.M41, current.M42, current.M43, current.M44
		};
		gl().UniformMatrix4(getLocation(), 1, false, data);
	}
}
