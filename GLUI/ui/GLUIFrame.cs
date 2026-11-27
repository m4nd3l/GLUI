using GLUI.input;
using GLUI.rendering;

namespace GLUI.ui;

public class GLUIFrame : IDisposable {
    public IReadOnlyList<GLUIComponent> componentsList => components.AsReadOnly();
    internal UI? owner { get; private set; }
    private List<GLUIComponent> components { get; } = [];

    public int zIndex {
        get => z;
        private set {
            if (z.Equals(value)) return;
            z = value;
            owner?.invalidate();
        }
    }
    private bool disposed { get; set; }
    private int z;
    
    public GLUIFrame(int zIndex = 0, params GLUIComponent[] toAdd) {
        this.zIndex = zIndex;
        addComponent(toAdd);
    }
    
    public void addComponent(List<GLUIComponent> toAdd) => addComponent(toAdd.ToArray());
    public void addComponent(params GLUIComponent[] toAdd) {
        ObjectDisposedException.ThrowIf(disposed, typeof(GLUIFrame));
        foreach (var component in toAdd) {
            if (component.parent != null || component.frame != null || component.disposed)
                throw new InvalidOperationException("Component is disposed or already attached.");
            component.attachFrame(this);
            components.Add(component);
        }
        owner?.invalidateLayout();
    }
    public void removeComponent(GLUIComponent component) {
        if (!components.Remove(component)) return;
        component.attachFrame(null);
        owner?.invalidateLayout();
    }
    
    public void resize(int width, int height) {
        foreach (GLUIComponent component in components.ToArray()) component.resize(width, height);
    }
    
    public void push(GLUIRenderer renderer) {
        foreach (GLUIComponent component in components.OrderBy(component => component.zIndex).ToArray())
            if (component.frame == this && component.parent == null) component.render(renderer);
    }
    
    public bool update(InputMap? input, double delta, bool mouseCaptured) {
        var ordered = components.OrderBy(component => component.zIndex).ToArray();
        bool captured = mouseCaptured;
        for (int i = ordered.Length - 1; i >= 0; i--)
            if (ordered[i].frame == this && ordered[i].parent == null)
                captured |= ordered[i].update(input, delta, captured);
        return captured;
    }
    
    internal void attach(UI? newOwner) {
        ObjectDisposedException.ThrowIf(disposed, typeof(GLUIFrame));
        owner = newOwner;
    }
    
    public void Dispose() {
        if (disposed) return;
        owner?.removeFrame(this);
        disposed = true;
        foreach (GLUIComponent component in components.ToArray()) component.Dispose();
        components.Clear();
    }
}
