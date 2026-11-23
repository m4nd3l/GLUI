using Silk.NET.OpenGL;
using StbImageSharp;

namespace GLUI.rendering.opengl;

public unsafe class Texture : RenderObject {
	public static readonly TexturePath GLUI_LOGO = new ("textures/glui_logo.png");
	
	private readonly uint texture;

	public Texture(GL glInstance, TexturePath path) : base(glInstance) {
		texture = gl().GenTexture();
		gl().ActiveTexture(TextureUnit.Texture0);
		bind();
		
		var (success, error) = readFile(path.getPath(), out byte[] image);
		if (!success) {
			disposeImplementation();
			throw new ArgumentException($"path argument with value {path.getPath()} is not valid: {error}");
		}
		
		ImageResult result = ImageResult.FromMemory(image, ColorComponents.RedGreenBlueAlpha);
		fixed(byte* pointer = result.Data)
			gl().TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.Rgba, (uint) result.Width, (uint) result.Height, 
				0, PixelFormat.Rgba, PixelType.UnsignedByte, pointer);
		
		gl().TexParameterI(GLEnum.Texture2D, GLEnum.TextureWrapS, (int) TextureWrapMode.Repeat);
		gl().TexParameterI(GLEnum.Texture2D, GLEnum.TextureWrapT, (int) TextureWrapMode.Repeat);
		gl().TexParameterI(GLEnum.Texture2D, GLEnum.TextureMinFilter, (int) TextureMinFilter.Nearest);
		gl().TexParameterI(GLEnum.Texture2D, GLEnum.TextureMagFilter, (int) TextureMagFilter.Nearest);
		
		unbind();
	}

	public Texture(GL glInstance, uint width, uint height, byte[] data) : base(glInstance) {
		texture = gl().GenTexture();
		gl().ActiveTexture(TextureUnit.Texture0);
		bind();
		fixed(byte* pointer = data)
			gl().TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.Rgba, width, height, 
				0, PixelFormat.Rgba, PixelType.UnsignedByte, pointer);
		
		gl().TexParameterI(GLEnum.Texture2D, GLEnum.TextureWrapS, (int) TextureWrapMode.Repeat);
		gl().TexParameterI(GLEnum.Texture2D, GLEnum.TextureWrapT, (int) TextureWrapMode.Repeat);
		gl().TexParameterI(GLEnum.Texture2D, GLEnum.TextureMinFilter, (int) TextureMinFilter.Nearest);
		gl().TexParameterI(GLEnum.Texture2D, GLEnum.TextureMagFilter, (int) TextureMagFilter.Nearest);
		
		unbind();
	}
	
	public sealed override void bind() => gl().BindTexture(TextureTarget.Texture2D, texture);
	public sealed override void unbind() => gl().BindTexture(TextureTarget.Texture2D, 0);

	public sealed override void disposeImplementation() {
		unbind();
		gl().DeleteTexture(texture);
	}

	public class TexturePath {
		private readonly string path;
		internal TexturePath(string path) => this.path = "GLUI.resources." + path;
		public string getPath() => path;
	}
}
