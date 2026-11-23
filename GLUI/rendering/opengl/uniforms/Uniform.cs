using Silk.NET.OpenGL;

namespace GLUI.rendering.opengl.uniforms;

public abstract unsafe class Uniform {
	private GL glInstance;
	private uint shaderProgram;
	private int location;
	private void* value;

	protected Uniform(GL gl, string name, uint shaderProgram, void* value) {
		glInstance = gl;
		this.shaderProgram = shaderProgram;
		this.value = value;
		setLocation(name);
	}

	public abstract void upload();

	protected GL gl() => glInstance;
	protected int getLocation() => location;
	protected void* getValue() => value;
	protected void setValue(void* obj) => value = obj;

	private void setLocation(string name) {
		location = gl().GetUniformLocation(shaderProgram, name);
		if (location is -1) throw new ArgumentException($"Uniform {name} not found");
	}
}
