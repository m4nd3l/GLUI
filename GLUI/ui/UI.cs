using GLUI.input;
using GLUI.rendering;
using GLUI.ui.utils;

namespace GLUI.ui;

public class UI : IDisposable {
    public IReadOnlyList<GLUIFrame> framesList => frames.AsReadOnly();
    private GLUIRenderer renderer { get; }
    private List<GLUIFrame> frames { get; }  = [];
    private UIState state { get; }  = new();
    private bool disposed { get; set; }
    private bool hasSize { get; set; }
    private int width { get; set; }
    private int height { get; set; }

    public UI(GLUIRenderer renderer, params GLUIFrame[] toAdd) {
        this.renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
        addFrame(toAdd);
    }
    
    public void addFrame(List<GLUIFrame> toAdd) => addFrame(toAdd.ToArray());
    public void addFrame(params GLUIFrame[] toAdd) {
        ObjectDisposedException.ThrowIf(disposed, typeof(UI));
        foreach (GLUIFrame frame in toAdd) {
            if (frame.owner != null) throw new InvalidOperationException("Frame is already attached.");
            frame.attach(this);
            frames.Add(frame);
        }
        
        invalidateLayout();
    }
    public void removeFrame(GLUIFrame frame) {
        if (!frames.Remove(frame)) return;
        frame.attach(null);
        
        invalidateLayout();
    }
    
    public void invalidate() => state.dirty(true);
    public void invalidateLayout() => state.invalidateLayout();
    
    public void resize(int newWidth, int newHeight) {
        ObjectDisposedException.ThrowIf(disposed, typeof(UI));
        if (newWidth <= 0 || newHeight <= 0) return;
        renderer.updateProjection(newWidth, newHeight);
        width = newWidth;
        height = newHeight;
        hasSize = true;
        invalidateLayout();
        ensureLayout();
    }
    
    public void update(InputMap? input, double delta) {
        ObjectDisposedException.ThrowIf(disposed, typeof(UI));
        if (!hasSize) return;
        
        ensureLayout();
        bool captured = false;
        var ordered = frames.OrderBy(frame => frame.zIndex).ToArray();
        
        for (int i = ordered.Length - 1; i >= 0; i--)
            if (ordered[i].owner == this) captured |= ordered[i].update(input, delta, captured);
        
        ensureLayout();
        if (!state.isDirty) return;
        state.dirty(false);
        
        try {
            renderer.beginBatch();
            foreach (GLUIFrame frame in frames.OrderBy(frame => frame.zIndex).ToArray())
                if (frame.owner == this) frame.push(renderer);
            renderer.endBatch();
        } catch {
            state.dirty(true);
            throw;
        }
    }
    
    private void ensureLayout() {
        if (!state.isLayoutDirty || !hasSize) return;
        state.validateLayout();
        foreach (GLUIFrame frame in frames.ToArray())
            frame.resize(width, height);
    }
        
    public void Dispose() {
        if (disposed) return;
        disposed = true;
        foreach (var frame in frames.ToArray()) frame.Dispose();
        frames.Clear();
        GC.SuppressFinalize(this);
    }
}
