using GLUI.input;
using GLUI.rendering;

namespace GLUI.ui;

public unsafe class GLUIFrame : IDisposable {
	private List<GLUIComponent> components;
	private int zIndex;
	private bool* dirty;

	public GLUIFrame(int zIndex, bool* dirtyPointer, params GLUIComponent[] toAdd) {
		this.zIndex = zIndex;
		components = [.. toAdd];
		dirty = dirtyPointer;
	}
	
	public void push(GLUIRenderer renderer) => components.ForEach(component => { component.push(renderer); component.finish(renderer); });
	
	public bool update(InputMap? input, double delta, bool mouseCaptured, bool* dirtyPointer) {
		if (dirtyPointer != null && dirtyPointer != dirty)
			dirty = dirtyPointer; 
		
		var capturedInFrame = false;
		for (int i = components.Count - 1; i >= 0; i--)
			if (components[i].update(input, delta, mouseCaptured || capturedInFrame, dirtyPointer)) capturedInFrame = true;
		return capturedInFrame;
	}
	
	public void removeComponent(GLUIComponent toRemove) { components.Remove(toRemove); sort(); }
	public void addComponent(params List<GLUIComponent> toAdd) { components.AddRange(toAdd); sort(); }
	
	public int z() => zIndex;

	public void resize(int width, int height) {
		if (components.Count is 0) return;
		foreach (var component in components)
			component.resize(width, height);
	}
	
	public void Dispose() {
		components.ForEach(component => component.Dispose());
		components.Clear();
	}
	
	private void sort() => components.Sort(new GLUIComponentComparer());

	public class GLUIComponentComparer : IComparer<GLUIComponent> {
		public int Compare(GLUIComponent? x, GLUIComponent? y) {
			if (x is null && y is null) return 0;
			if (x is null) return -1;
			if (y is null) return 1;
			if (x.z().Equals(y.z())) return 0;
			if (x.z() < y.z()) return -1;
			return 1;
		}
	}
}
