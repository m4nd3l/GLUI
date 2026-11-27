using GLUI.rendering.opengl.uniforms;
using Silk.NET.OpenGL;

namespace GLUI.rendering.opengl;

public class ShaderProgram : RenderObject {
	public uint shaderProgram { get; }
	private List<Uniform> uniforms { get; } = [];
	private string vertexShaderPath;
	private string vertexShaderSource;
	private string fragmentShaderPath;
	private string fragmentShaderSource;

	public ShaderProgram(GL gl, string vertexShaderPath, string fragmentShaderPath) : base(gl) {
		this.vertexShaderPath = vertexShaderPath;
		this.fragmentShaderPath = fragmentShaderPath;

		(bool isVertexReadSuccess, string vertexReadError) = readFile(vertexShaderPath, out vertexShaderSource);
		if (!isVertexReadSuccess) throw new ArgumentException($"vertexShaderPath argument with value {vertexShaderPath} is not valid: {vertexReadError}");

		(bool isFragmentReadSuccess, string fragmentReadError) = readFile(fragmentShaderPath, out fragmentShaderSource);
		if (!isFragmentReadSuccess) throw new ArgumentException($"fragmentShaderPath argument with value {fragmentShaderPath} is not valid: {fragmentReadError}");

		uint vertexShader = 0;
		uint fragmentShader = 0;

		try {
			vertexShader = compileShader(ShaderType.VertexShader, vertexShaderSource, "vertex");
			fragmentShader = compileShader(ShaderType.FragmentShader, fragmentShaderSource, "fragment");

			shaderProgram = gl.CreateProgram();
			gl.AttachShader(shaderProgram, vertexShader);
			gl.AttachShader(shaderProgram, fragmentShader);
			gl.LinkProgram(shaderProgram);

			gl.GetProgram(shaderProgram, ProgramPropertyARB.LinkStatus, out int linkStatus);
			if (linkStatus != (int)GLEnum.True) {
				string infoLog = gl.GetProgramInfoLog(shaderProgram);
				throw new ArgumentException($"An error occurred while linking the program: {infoLog}");
			}

			gl.DetachShader(shaderProgram, vertexShader);
			gl.DetachShader(shaderProgram, fragmentShader);
		} catch {
			if (shaderProgram != 0) gl.DeleteProgram(shaderProgram);
			throw;
		} finally {
			if (vertexShader != 0) gl.DeleteShader(vertexShader);
			if (fragmentShader != 0) gl.DeleteShader(fragmentShader);
		}
	}

	private uint compileShader(ShaderType shaderType, string shaderSource, string shaderLabel) {
		uint shader = gl.CreateShader(shaderType);
		gl.ShaderSource(shader, shaderSource);
		gl.CompileShader(shader);

		gl.GetShader(shader, ShaderParameterName.CompileStatus, out int compileStatus);
		if (compileStatus != (int)GLEnum.True) {
			string infoLog = gl.GetShaderInfoLog(shader);
			gl.DeleteShader(shader);
			throw new ArgumentException($"An error occurred while compiling the {shaderLabel} shader: {infoLog}");
		}

		return shader;
	}

	public void add(Uniform uniform) => uniforms.Add(uniform);
	public void remove(Uniform uniform) => uniforms.Remove(uniform);
	public void removeAll(Type toRemove) => uniforms.RemoveAll(uniform => uniform.GetType() == toRemove);
	public bool contains(Uniform uniform) => uniforms.Contains(uniform);
	public void upload() => uniforms.ForEach(uniform => uniform.upload());
	
	public sealed override void bind() => gl.UseProgram(shaderProgram);
	public sealed override void unbind() => gl.UseProgram(0);
	public sealed override void disposeImplementation() { unbind(); gl.DeleteProgram(shaderProgram); }
}