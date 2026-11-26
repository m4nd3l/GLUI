using GLUI.input;
using GLUI.rendering;
using GLUI.ui.utils;
using Silk.NET.Input;
using Silk.NET.Maths;

namespace GLUI.ui;

public abstract unsafe class GLUIComponent : IDisposable {
	private List<GLUIComponent> children;
	private Dimensions dimensions;
	private Vector2D<float> absolutePosition;
	private Alignment alignment;
	private Margin margin;
	private int zIndex;
	private UIState state;

	private bool onClickCalled;
	private bool onHoverCalled;

	protected GLUIComponent(Dimensions? dimensions = null, Alignment? alignment = null, Margin? margin = null, int zIndex = 0, params GLUIComponent[] toAdd) {
		dimensions ??= new Dimensions();
		alignment ??= Alignment.CENTER;
		margin ??= new Margin();
		this.dimensions = dimensions;
		absolutePosition = new Vector2D<float>();
		this.alignment = alignment;
		this.margin = margin;
		this.zIndex = zIndex;
		children = [.. toAdd];
		onClickCalled = false;
		onHoverCalled = false;
		state = new UIState();
	}

	public abstract void push(GLUIRenderer renderer);
	public void finish(GLUIRenderer renderer) => children.ForEach(child => { child.push(renderer); child.finish(renderer); });
	
	public bool update(InputMap? input, double delta, bool alreadyCaptured, UIState dirtyPointer) {
		if (dirtyPointer != null && dirtyPointer != state) state = dirtyPointer;
		
		beforeUpdate(input, delta, alreadyCaptured);
		
		var hovered = !alreadyCaptured && isHovered(input);
		var capturedInChildren = false;

		for (var i = children.Count - 1; i >= 0; i--)
			if (children[i].update(input, delta, alreadyCaptured || hovered || capturedInChildren, dirtyPointer))
				capturedInChildren = true;

		if (hovered) {
			if (input?.isPressed(MouseButton.Left) ?? false) {
				if (!onClickCalled) onClick(input.getMousePosition());
				onClickCalled = true;
				onClickHold(input.getMousePosition());
			} else onClickCalled = false;

			if (!onHoverCalled && !onClickCalled) onHover(input?.getMousePosition() ?? new Vector2D<float>(0.0f));
			onHoverCalled = true;
			onHoverHold(input?.getMousePosition() ?? new Vector2D<float>(0.0f));
		} else {
			onClickCalled = false;
			onHoverCalled = false;
		}

		afterUpdate(input, delta, alreadyCaptured);
		
		return hovered || capturedInChildren;
	}
	
	public void removeComponent(GLUIComponent component) { children.Remove(component); sort(); }
	public void addComponent(params List<GLUIComponent> toAdd) {
		Vector2D<float> size = dimensions.getSize();
		toAdd.ForEach(component => component.setParentSize(size));
		children.AddRange(toAdd);
		sort();
	}
	
	protected virtual void onClick(Vector2D<float> mousePosition) { }
	protected virtual void onClickHold(Vector2D<float> mousePosition) { }
	protected virtual void onHover(Vector2D<float> mousePosition) { }
	protected virtual void onHoverHold(Vector2D<float> mousePosition) { }
	protected virtual void beforeUpdate(InputMap? input, double delta, bool alreadyCaptured) { }
	protected virtual void afterUpdate(InputMap? input, double delta, bool alreadyCaptured) { }
	
	public int z() => zIndex;
	public Vector2D<float> getAbsolutePosition() => absolutePosition;
	public Alignment getAlignment() => alignment;
	public Dimensions getDimensions() => dimensions;
	public Margin getMargin() => margin;
	
	public void setZIndex(int z) => zIndex = z;
	public void setAlignment(Alignment newAlignment) => alignment = newAlignment;
	public void setDimensions(Dimensions newDimensions) => dimensions = newDimensions;
	public void setMargin(Margin newMargin) => margin = newMargin;
	public void setDirty() => state.dirty(true);
	
	public void resize(int width, int height, float parentX = 0.0f, float parentY = 0.0f) {
		dimensions.setParent(new Vector2D<float>(width, height));
		dimensions.calculateSize();
		children.ForEach(child => child.resize(width, height, absolutePosition.X, absolutePosition.Y));
	}
	
	private void sort() => children.Sort(new GLUIFrame.GLUIComponentComparer());
	private void setParentSize(Vector2D<float> size) => dimensions.setParent(size);
	public void Dispose() {
		children.ForEach(child => child.Dispose());
		children.Clear();
	}
	private bool isHovered(InputMap? input) {
		Vector2D<float> mouse = input?.getMousePosition() ?? new Vector2D<float>(0.0f);
		Vector2D<float> size = dimensions.getSize();
		return mouse.X >= absolutePosition.X &&
		       mouse.X <= absolutePosition.X + size.X &&
		       mouse.Y >= absolutePosition.Y &&
		       mouse.Y <= absolutePosition.Y + size.Y;
	}
}
