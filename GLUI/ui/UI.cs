using GLUI.input;
using GLUI.rendering;
using System.Runtime.InteropServices;

namespace GLUI.ui;

public unsafe class UI : IDisposable {
	private GLUIRenderer renderer;
	private List<GLUIFrame> frames;
	private bool* dirty;

	public UI(GLUIRenderer renderer, params GLUIFrame[] toAdd) {
		this.renderer = renderer;
		frames = new List<GLUIFrame>(toAdd);
		dirty = (bool*) NativeMemory.Alloc(sizeof(bool));
		*dirty = false;
	}

	public void update(InputMap? input, double delta) {
		var mouseCaptured = false;
		for (int i = frames.Count - 1; i >= 0; i--)
			if (frames[i].update(input, delta, mouseCaptured, dirty)) mouseCaptured = true;
		
		if (*dirty) push();
		*dirty = false;
	}
	
	public void removeFrame(GLUIFrame frame) { frame.Dispose(); frames.Remove(frame); sort(); }
	public void addFrame(params List<GLUIFrame> toAdd) { frames.AddRange(toAdd); sort(); }
	
	public void resize(int width, int height) {
		if (frames.Count is 0) return;
		foreach (var frame in frames)
			frame.resize(width, height);
	}
	
	public void Dispose() {
		frames.ForEach(frame => frame.Dispose());
		frames.Clear();
		NativeMemory.Free(dirty);
	}

	private void push() => frames.ForEach(frame => frame.push(renderer));
	private void sort() => frames.Sort(new FrameComparer());
	
	private class FrameComparer : IComparer<GLUIFrame> {
		public int Compare(GLUIFrame? x, GLUIFrame? y) {
			if (x is null && y is null) return 0;
			if (x is null) return -1;
			if (y is null) return 1;
			if (x.z().Equals(y.z())) return 0;
			if (x.z() < y.z()) return -1;
			return 1;
		}
	}
}
