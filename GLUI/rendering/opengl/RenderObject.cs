using Silk.NET.OpenGL;
using System.Reflection;

namespace GLUI.rendering.opengl;

public abstract class RenderObject : IDisposable {
	internal GL gl { get; }
	private bool disposed { get; set; }

	protected RenderObject(GL gl) => this.gl = gl;

	protected (bool success, string error) readFile(string path, out byte[] content) {
		content = Array.Empty<byte>();
		try {
			using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(path);

			if (stream is null) return (false, $"Resource not found: {path}");

			using var memoryStream = new MemoryStream();
			stream.CopyTo(memoryStream);
			content = memoryStream.ToArray();
			return (true, string.Empty);
		} catch (Exception exception) { return (false, exception.Message); }
	}
	protected (bool success, string error) readFile(string path, out string content) {
		content = string.Empty;
		try {
			using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(path);

			if (stream is null) return (false, $"Resource not found: {path}");

			using var reader = new StreamReader(stream);
			content = reader.ReadToEnd();
			return (true, string.Empty);
		} catch (Exception exception) { return (false, exception.Message); }
	}
	
	public abstract void bind();
	public abstract void unbind();
	public abstract void disposeImplementation();
	public void Dispose() {
		if (disposed) return;
		disposeImplementation();
		disposed = true;
		GC.SuppressFinalize(this);
	}
}
