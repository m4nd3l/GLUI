using GLUI.rendering.opengl;
using GLUI.rendering.opengl.attributes;
using GLUI.rendering.opengl.uniforms;
using GLUI.ui.utils;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using System.Runtime.InteropServices;
using Texture = GLUI.rendering.opengl.Texture;

namespace GLUI.rendering;

public unsafe partial class Renderer : IDisposable {
	private const int maxTextureSlots = 16;
	
	private GL gl;
	private VAO vao;
	private VBO vbo;
	private EBO ebo;
	private ShaderProgram shader;
	private Matrix4X4<float>* projection;
	
	private List<float>? vertices;
	private List<uint>? indices;

	private Texture?[] textures;
	private int texturesCount;
	private Texture whiteTexture;
	
	private bool disposed;

	public Renderer(GL gl) {
		this.gl = gl;
		
		gl.Enable(EnableCap.Blend);
		gl.BlendFunc(
			BlendingFactor.SrcAlpha,
			BlendingFactor.OneMinusSrcAlpha
		);
		
		vao = new VAO(gl);
		vbo = new VBO(gl);
		ebo = new EBO(gl);
		
		vbo.bind();
		vao.addAttributes(
			new Vec2fAttribute(gl), // vec2 position
			new Vec2fAttribute(gl), // vec2 texture coordinates
			new Vec2fAttribute(gl), // vec2 size
			new Vec4fAttribute(gl), // vec4 filler
			new Vec4fAttribute(gl), // vec4 border
			new IntAttribute(gl),   // int  texture slot
			new Vec4fAttribute(gl), // vec4 shape arguments
			new IntAttribute(gl));  // int  start left
		vbo.unbind();
		vao.unbind();
		
		shader = new ShaderProgram(gl, "GLUI.resources.shaders.base.vert", "GLUI.resources.shaders.base.frag");
		shader.bind();
		projection = (Matrix4X4<float>*) NativeMemory.Alloc((nuint) sizeof(Matrix4X4<float>));
		*projection = Matrix4X4<float>.Identity;
		shader.add(new Matrix4fUniform(gl, "uProjection", shader.getProgram(), projection));
		shader.unbind();
		
		vertices = [ ];
		indices = [ ];
		
		textures = new Texture?[maxTextureSlots];
		texturesCount = 0;
		
		byte[] rgba = [255, 255, 255, 255];
		whiteTexture = new Texture(gl, 1, 1, rgba);
	}

	public void updateProjection(int width, int height) {
		if (projection is null) return;
		*projection = Matrix4X4.CreateOrthographic(width, height, -1f, 1f);
	}
	
	public void uploadGPU() {
		if (vertices is not { Count: > 0 } || indices is not { Count: > 0 }) return;
		
		vao.bind();

		vbo.bind();
		vbo.updateData([.. vertices]);

		ebo.bind();
		ebo.updateData([.. indices]);

		vbo.unbind();
		vao.unbind();
	}
	
	public void render() {
		if (vertices is not { Count: > 0 } || indices is not { Count: > 0 }) return;
		
		vao.bind();
		
		shader.bind();
		shader.upload();
		
		gl.DrawElements(PrimitiveType.Triangles, (uint) indices.Count, DrawElementsType.UnsignedInt, (void*) 0);
	}

	public void Dispose() {
		if (disposed) return;

		if (projection is not null) {
			NativeMemory.Free(projection);
			projection = null;
		}
		
		vao.Dispose();
		vbo.Dispose();
		ebo.Dispose();
		shader.Dispose();
		foreach (var texture in textures)
			texture?.Dispose();
		whiteTexture.Dispose();
		disposed = true;
		GC.SuppressFinalize(this);
	}
}
