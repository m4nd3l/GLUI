using Silk.NET.OpenGL;
using StbImageSharp;

namespace GLUI.rendering.opengl;

public unsafe class Texture : RenderObject {
    public static readonly TexturePath GLUI_LOGO = new("textures.glui_logo.png");

    private uint texture { get; }
    public uint width { get; }
    public uint height { get; }
    
    public Texture(GL gl, TexturePath path) : base(gl) {
        ArgumentNullException.ThrowIfNull(path);
        var (success, error) = readFile(path.path, out byte[] bytes);
        if (!success) throw new ArgumentException($"Texture resource '{path.path}' was not found: {error}", nameof(path));
        StbImage.stbi_set_flip_vertically_on_load(1);
        ImageResult image = ImageResult.FromMemory(bytes, ColorComponents.RedGreenBlueAlpha);
        width = (uint) image.Width;
        height = (uint) image.Height;
        texture = gl.GenTexture();
        uploadRGBA(image.Data);
    }

    public Texture(GL gl, string filePath) : base(gl) {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        ImageResult image = ImageResult.FromMemory(File.ReadAllBytes(filePath), ColorComponents.RedGreenBlueAlpha);
        width = (uint)image.Width;
        height = (uint)image.Height;
        texture = gl.GenTexture();
        uploadRGBA(image.Data);
    }
    
    public Texture(GL gl, uint width, uint height, byte[] rgba) : base(gl) {
        if (width == 0 || height == 0) throw new ArgumentOutOfRangeException(nameof(width));
        ArgumentNullException.ThrowIfNull(rgba);
        ulong expected = (ulong)width * height * 4UL;
        if ((ulong)rgba.Length != expected)
            throw new ArgumentException($"Expected {expected} RGBA bytes, got {rgba.Length}.", nameof(rgba));
        this.width = width;
        this.height = height;
        texture = gl.GenTexture();
        uploadRGBA(rgba);
    }

    private void uploadRGBA(byte[] rgba) {
        gl.ActiveTexture(TextureUnit.Texture0);
        bind();
        fixed (byte* pointer = rgba) {
            gl.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.Rgba,
                width, height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, pointer);
        }
        gl.TexParameterI(GLEnum.Texture2D, GLEnum.TextureMinFilter, (int)TextureMinFilter.Nearest);
        gl.TexParameterI(GLEnum.Texture2D, GLEnum.TextureMagFilter, (int)TextureMagFilter.Nearest);
        gl.TexParameterI(GLEnum.Texture2D, GLEnum.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
        gl.TexParameterI(GLEnum.Texture2D, GLEnum.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
        unbind();
    }
    
    public void setFiltering(TextureMinFilter minFilter, TextureMagFilter magFilter) {
        gl.ActiveTexture(TextureUnit.Texture0);
        bind();
        gl.TexParameterI(GLEnum.Texture2D, GLEnum.TextureMinFilter, (int) minFilter);
        gl.TexParameterI(GLEnum.Texture2D, GLEnum.TextureMagFilter, (int) magFilter);
        unbind();
    }

    public void setWrap(TextureWrapMode wrapS, TextureWrapMode wrapT) {
        gl.ActiveTexture(TextureUnit.Texture0);
        bind();
        gl.TexParameterI(GLEnum.Texture2D, GLEnum.TextureWrapS, (int) wrapS);
        gl.TexParameterI(GLEnum.Texture2D, GLEnum.TextureWrapT, (int) wrapT);
        unbind();
    }

    public sealed override void bind() => gl.BindTexture(TextureTarget.Texture2D, texture);
    public sealed override void unbind() => gl.BindTexture(TextureTarget.Texture2D, 0);
    public sealed override void disposeImplementation() {
        unbind();
        gl.DeleteTexture(texture);
    }

    public class TexturePath {
        public string path { get; }
        public TexturePath(string relativeResourcePath) {
            path = relativeResourcePath.StartsWith("GLUI.resources.", StringComparison.Ordinal)
                ? relativeResourcePath
                : "GLUI.resources." + relativeResourcePath;
        }
    }
}
