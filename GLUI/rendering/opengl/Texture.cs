using Silk.NET.OpenGL;
using StbImageSharp;

namespace GLUI.rendering.opengl;

public unsafe class Texture : RenderObject {
    public static readonly TexturePath GLUI_LOGO = new("textures.glui_logo.png");

    private readonly uint texture;
    private readonly uint width;
    private readonly uint height;
    
    public Texture(GL glInstance, TexturePath path) : base(glInstance) {
        ArgumentNullException.ThrowIfNull(path);
        var (success, error) = readFile(path.getPath(), out byte[] bytes);
        if (!success) throw new ArgumentException($"Texture resource '{path.getPath()}' was not found: {error}", nameof(path));
        StbImage.stbi_set_flip_vertically_on_load(1);
        ImageResult image = ImageResult.FromMemory(bytes, ColorComponents.RedGreenBlueAlpha);
        width = (uint) image.Width;
        height = (uint) image.Height;
        texture = gl().GenTexture();
        uploadRGBA(image.Data);
    }

    public Texture(GL glInstance, string filePath) : base(glInstance) {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        ImageResult image = ImageResult.FromMemory(File.ReadAllBytes(filePath), ColorComponents.RedGreenBlueAlpha);
        width = (uint)image.Width;
        height = (uint)image.Height;
        texture = gl().GenTexture();
        uploadRGBA(image.Data);
    }
    
    public Texture(GL glInstance, uint width, uint height, byte[] rgba) : base(glInstance) {
        if (width == 0 || height == 0) throw new ArgumentOutOfRangeException(nameof(width));
        ArgumentNullException.ThrowIfNull(rgba);
        ulong expected = (ulong)width * height * 4UL;
        if ((ulong)rgba.Length != expected)
            throw new ArgumentException($"Expected {expected} RGBA bytes, got {rgba.Length}.", nameof(rgba));
        this.width = width;
        this.height = height;
        texture = gl().GenTexture();
        uploadRGBA(rgba);
    }

    private void uploadRGBA(byte[] rgba) {
        gl().ActiveTexture(TextureUnit.Texture0);
        bind();
        fixed (byte* pointer = rgba) {
            gl().TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.Rgba,
                width, height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, pointer);
        }
        gl().TexParameterI(GLEnum.Texture2D, GLEnum.TextureMinFilter, (int)TextureMinFilter.Nearest);
        gl().TexParameterI(GLEnum.Texture2D, GLEnum.TextureMagFilter, (int)TextureMagFilter.Nearest);
        gl().TexParameterI(GLEnum.Texture2D, GLEnum.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
        gl().TexParameterI(GLEnum.Texture2D, GLEnum.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
        unbind();
    }

    public uint getWidth() => width;
    public uint getHeight() => height;
    
    public void setFiltering(TextureMinFilter minFilter, TextureMagFilter magFilter) {
        gl().ActiveTexture(TextureUnit.Texture0);
        bind();
        gl().TexParameterI(GLEnum.Texture2D, GLEnum.TextureMinFilter, (int) minFilter);
        gl().TexParameterI(GLEnum.Texture2D, GLEnum.TextureMagFilter, (int) magFilter);
        unbind();
    }

    public void setWrap(TextureWrapMode wrapS, TextureWrapMode wrapT) {
        gl().ActiveTexture(TextureUnit.Texture0);
        bind();
        gl().TexParameterI(GLEnum.Texture2D, GLEnum.TextureWrapS, (int) wrapS);
        gl().TexParameterI(GLEnum.Texture2D, GLEnum.TextureWrapT, (int) wrapT);
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
        public TexturePath(string relativeResourcePath) {
            path = relativeResourcePath.StartsWith("GLUI.resources.", StringComparison.Ordinal)
                ? relativeResourcePath
                : "GLUI.resources." + relativeResourcePath;
        }
        public string getPath() => path;
    }
}
