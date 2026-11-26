using GLUI.rendering.opengl;
using GLUI.rendering.opengl.attributes;
using GLUI.rendering.opengl.uniforms;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using System.Runtime.InteropServices;
using Texture = GLUI.rendering.opengl.Texture;

namespace GLUI.rendering;

public unsafe partial class GLUIRenderer : IDisposable {
    private const int maxTextureSlots = 16;
    
    private const int vertexWords = 22;

    private readonly GL gl;
    private readonly VAO vao;
    private readonly VBO vbo;
    private readonly EBO ebo;
    private readonly ShaderProgram shader;
    private Matrix4X4<float>* projection;

    private readonly List<float> vertices = [];
    private readonly List<uint> indices = [];
    private readonly List<TextureGroup> textureGroups = [];
    private bool disposed;
    
    private sealed class TextureGroup(int startIndex) {
        public readonly int firstIndex = startIndex;
        public readonly List<Texture> textures = [];
    }

    public GLUIRenderer(GL gl) {
        this.gl = gl;
        vao = new VAO(gl);
        vbo = new VBO(gl);
        ebo = new EBO(gl);

        gl.Enable(EnableCap.Blend);
        gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

        vbo.bind();
        vao.addAttributes(
            new Vec2fAttribute(gl), // 0: position
            new Vec2fAttribute(gl), // 1: local UV (always 0..1 for SDF)
            new Vec2fAttribute(gl), // 2: texture UV (may be cropped)
            new Vec2fAttribute(gl), // 3: shape size in pixels
            new Vec4fAttribute(gl), // 4: fill tint
            new Vec4fAttribute(gl), // 5: border / track color
            new IntAttribute(gl),   // 6: texture slot (-1 == no texture)
            new Vec4fAttribute(gl), // 7: [type, thickness, progressOrLength, radius%]
            new IntAttribute(gl));  // 8: direction (1 forward, 0 reversed)
        vbo.unbind();
        vao.unbind();

        if (vao.getVertexComponents() != vertexWords)
            throw new InvalidOperationException("Renderer vertex layout does not match its shader.");

        shader = new ShaderProgram(gl, "GLUI.resources.shaders.base.vert", "GLUI.resources.shaders.base.frag");
        projection = (Matrix4X4<float>*) NativeMemory.Alloc((nuint) sizeof(Matrix4X4<float>));
        *projection = Matrix4X4<float>.Identity;

        shader.bind();
        shader.add(new Matrix4fUniform(gl, "uProjection", shader.getProgram(), projection));
        for (int i = 0; i < maxTextureSlots; i++) {
            Uniform1 uniform = new Uniform1(gl, $"uTextures[{i}]", shader.getProgram(), &i);
            shader.add(uniform);
            uniform.upload();
        }
        shader.unbind();

        beginBatch();
    }

    public void updateProjection(int width, int height) {
        if (projection is null || width <= 0 || height <= 0) return;
        *projection = Matrix4X4.CreateOrthographic(width, height, -1f, 1f);
    }
    
    public void beginBatch() {
        throwIfDisposed();
        vertices.Clear();
        indices.Clear();
        textureGroups.Clear();
        textureGroups.Add(new TextureGroup(0));
    }

    public void endBatch() => uploadGPU();
    
    public void uploadGPU() {
        throwIfDisposed();
        vao.bind();
        vbo.bind();
        
        vbo.updateData([.. vertices]);
        ebo.bind();
        ebo.updateData([.. indices]);

        vbo.unbind();
        vao.unbind();
    }
    
    public int registerTexture(Texture texture) {
        throwIfDisposed();
        ArgumentNullException.ThrowIfNull(texture);

        TextureGroup group = textureGroups[^1];
        for (int i = 0; i < group.textures.Count; i++)
            if (ReferenceEquals(group.textures[i], texture)) return i;

        if (group.textures.Count == maxTextureSlots) {
            group = new TextureGroup(indices.Count);
            textureGroups.Add(group);
        }

        group.textures.Add(texture);
        return group.textures.Count - 1;
    }

    private int resolveTextureSlot(Texture? texture, int slot = -1) {
        if (texture is not null) {
            if (slot >= 0)
                throw new ArgumentException("Specify either texture or textureSlot, not both.");
            return registerTexture(texture);
        }

        if (slot < -1 || slot >= textureGroups[^1].textures.Count)
            throw new ArgumentOutOfRangeException(nameof(slot), "Texture slot was not registered in the current group.");
        return slot;
    }

    public void render() {
        throwIfDisposed();
        if (indices.Count == 0) return;

        vao.bind();
        shader.bind();
        shader.upload();

        for (int groupIndex = 0; groupIndex < textureGroups.Count; groupIndex++) {
            TextureGroup group = textureGroups[groupIndex];
            int nextIndex = groupIndex + 1 < textureGroups.Count
                ? textureGroups[groupIndex + 1].firstIndex
                : indices.Count;
            int count = nextIndex - group.firstIndex;
            if (count <= 0) continue;

            for (int i = 0; i < group.textures.Count; i++) {
                gl.ActiveTexture((TextureUnit)((int)TextureUnit.Texture0 + i));
                group.textures[i].bind();
            }

            gl.DrawElements(
                PrimitiveType.Triangles,
                (uint)count,
                DrawElementsType.UnsignedInt,
                (void*) (group.firstIndex * sizeof(uint)));
        }

        gl.ActiveTexture(TextureUnit.Texture0);
        shader.unbind();
        vao.unbind();
    }

    private void throwIfDisposed() {
        if (!disposed) return;

        throw new ObjectDisposedException(nameof(GLUIRenderer));
    }

    public void Dispose() {
        if (disposed) return;
        disposed = true;
        if (projection is not null) {
            NativeMemory.Free(projection);
            projection = null;
        }
        vao.Dispose();
        vbo.Dispose();
        ebo.Dispose();
        shader.Dispose();

        GC.SuppressFinalize(this);
    }
}
