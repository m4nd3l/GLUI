using GLUI.input;
using GLUI.rendering;
using GLUI.ui.utils;
using Silk.NET.Input;
using Silk.NET.Maths;

namespace GLUI.ui;

public abstract class GLUIComponent : IDisposable {
    public IReadOnlyList<GLUIComponent> childrenList => children.AsReadOnly();
    public List<GLUIComponent> children { get; } = [];
    public Dimensions dimensions {
        get => dimensionsReference;
        set {
            if (dimensionsReference.Equals(value)) return;
            dimensionsReference.Changed -= invalidateLayout;
            dimensionsReference = value;
            dimensionsReference.Changed += invalidateLayout;
            invalidateLayout();
        }
    }
    public Vector2D<float> absolutePosition { get; private set; }
    public Alignment alignment {
        get => alignmentReference;
        set {
            if (alignmentReference.Equals(value)) return;
            alignmentReference = value; 
            invalidateLayout();
        }
    }
    public Margin margin {
        get => marginReference;
        set {
            if (marginReference.Equals(value)) return;
            marginReference.Changed -= invalidateLayout;
            marginReference = value;
            marginReference.Changed += invalidateLayout;
            invalidateLayout();
        }
    }
    public int zIndex {
        get => z;
        set {
            if (z.Equals(value)) return;
            z = value;
            setDirty();
        }
    }
    public bool disposed { get; private set; }
    internal GLUIComponent? parent { get; private set; }
    internal GLUIFrame? frame { get; private set; }
    protected virtual bool acceptsMouse => true;
    private bool hoveredLast { get; set; }
    private bool pressedHere { get; set; }
    private bool wasDown { get; set; }
    private Dimensions dimensionsReference;
    private Alignment alignmentReference;
    private Margin marginReference;
    private int z;

    protected GLUIComponent(Dimensions? dimensions = null, Alignment? alignment = null, Margin? margin = null, int zIndex = 0, params GLUIComponent[] toAdd) {
        this.dimensions = dimensions ?? new Dimensions();
        this.alignment = alignment ?? Alignment.CENTER;
        this.margin = margin ?? new Margin();
        this.zIndex = zIndex;
        this.dimensions.Changed += invalidateLayout;
        this.margin.Changed += invalidateLayout;
        addComponent(toAdd);
    }

    abstract protected void push(GLUIRenderer renderer);
    protected virtual void onClick(Vector2D<float> mousePosition) { }
    protected virtual void onClickHold(Vector2D<float> mousePosition) { }
    protected virtual void onHover(Vector2D<float> mousePosition) { }
    protected virtual void onHoverHold(Vector2D<float> mousePosition) { }
    protected virtual void onHoverExit(Vector2D<float> mousePosition) { }
    protected virtual void beforeUpdate(InputMap? input, double delta, bool alreadyCaptured) { }
    protected virtual void afterUpdate(InputMap? input, double delta, bool alreadyCaptured) { }
    protected virtual void disposeResources() { }
    
    public void setDirty() => frame?.owner?.invalidate();
    public void invalidateLayout() => frame?.owner?.invalidateLayout();
    
    public void resize(int width, int height, float parentX = 0, float parentY = 0)
        => resize(new Vector2D<float>(width, height), new Vector2D<float>(parentX, parentY));
    
    public void addComponent(List<GLUIComponent> toAdd) => addComponent(toAdd.ToArray());
    public void addComponent(params GLUIComponent[] toAdd) {
        if (disposed) throw new ObjectDisposedException(nameof(GLUIComponent));
        foreach (GLUIComponent child in toAdd) {
            if (child.disposed || child.parent != null || child.frame != null)
                throw new InvalidOperationException("Component is disposed or already attached.");
            for (GLUIComponent? node = this; node != null; node = node.parent)
                if (ReferenceEquals(node, child)) throw new InvalidOperationException("Circular component hierarchy.");
            child.parent = this;
            child.attachFrame(frame);
            children.Add(child);
        }
        invalidateLayout();
    }
    public void removeComponent(GLUIComponent child) {
        if (!children.Remove(child)) return;
        child.parent = null;
        child.attachFrame(null);
        invalidateLayout();
    }
    
    public bool isHovered(InputMap? input) {
        if (input == null) return false;
        var mouse = input.getMousePosition();
        var size = dimensions.size;
        return mouse.X >= absolutePosition.X && mouse.X < absolutePosition.X + size.X &&
               mouse.Y >= absolutePosition.Y && mouse.Y < absolutePosition.Y + size.Y;
    }
    
    public void render(GLUIRenderer renderer) {
        if (disposed) return;
        push(renderer);
        foreach (GLUIComponent child in children.OrderBy(component => component.zIndex).ToArray())
            if (child.parent == this) child.render(renderer);
    }
    
    public void finish(GLUIRenderer renderer) {
        foreach (GLUIComponent child in children.OrderBy(component => component.zIndex).ToArray())
            if (child.parent == this) child.render(renderer);
    }
    
    public bool update(InputMap? input, double delta, bool alreadyCaptured) {
        if (disposed) return alreadyCaptured;
        beforeUpdate(input, delta, alreadyCaptured);
        if (disposed) return alreadyCaptured;
        bool captured = alreadyCaptured;
        var ordered = children.OrderBy(component => component.zIndex).ToArray();
        for (int i = ordered.Length - 1; i >= 0; i--)
            if (ordered[i].parent == this)
                captured |= ordered[i].update(input, delta, captured);

        bool hovered = !captured && acceptsMouse && isHovered(input);
        bool down = input?.isPressed(MouseButton.Left) ?? false;
        if (input != null) {
            var mouse = input.getMousePosition();
            if (hovered && !hoveredLast) onHover(mouse);
            if (!hovered && hoveredLast) onHoverExit(mouse);
            if (hovered) onHoverHold(mouse);
            if (hovered && down && !wasDown) {
                pressedHere = true;
                onClick(mouse);
            }
            if (pressedHere && down && hovered) onClickHold(mouse);
        }
        if (!down) pressedHere = false;
        if (hovered != hoveredLast) setDirty();
        hoveredLast = hovered;
        wasDown = down;
        afterUpdate(input, delta, captured || hovered);
        return captured || hovered;
    }
    
    internal void attachFrame(GLUIFrame? toAttach) {
        frame = toAttach;
        foreach (GLUIComponent child in children) child.attachFrame(frame);
    }

    protected virtual Vector2D<float> calculatePosition(Vector2D<float> origin, Vector2D<float> parentSize, Vector2D<float> size) 
        => origin + alignment.getOffset(dimensions, margin);

    private void resize(Vector2D<float> parentSize, Vector2D<float> parentPosition) {
        dimensions.setLayoutParent(parentSize);
        dimensions.calculateSize();
        var size = dimensions.size;
        absolutePosition = calculatePosition(parentPosition, parentSize, size);
        foreach (var child in children.ToArray()) child.resize(size, absolutePosition);
        setDirty();
    }
    
    public void Dispose() {
        if (disposed) return;
        disposed = true;
        dimensions.Changed -= invalidateLayout;
        margin.Changed -= invalidateLayout;
        if (parent != null) parent.removeComponent(this);
        else frame?.removeComponent(this);
        foreach (var child in children.ToArray()) child.Dispose();
        children.Clear();
        disposeResources();
        GC.SuppressFinalize(this);
    }
}
