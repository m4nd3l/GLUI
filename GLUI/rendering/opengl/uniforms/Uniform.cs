using Silk.NET.OpenGL;

namespace GLUI.rendering.opengl.uniforms;

public abstract unsafe class Uniform {
	internal GL gl { get; }
	internal int location { get; private set; }
	internal void* value { get; }
	private uint shaderProgram { get; }

	internal Uniform(GL gl, string name, uint shaderProgram, void* value) {
		this.gl = gl;
		this.shaderProgram = shaderProgram;
		this.value = value;
		setLocation(name);
	}

	public abstract void upload();

	private void setLocation(string name) {
		location = gl.GetUniformLocation(shaderProgram, name);
		if (location is -1) throw new ArgumentException($"Uniform {name} not found");
	}
}
