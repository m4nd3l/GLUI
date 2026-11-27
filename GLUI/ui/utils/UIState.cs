namespace GLUI.ui.utils;

public sealed class UIState {
	public bool isDirty { get; private set; } = true;
	public bool isLayoutDirty { get; private set; } = true;
	public void dirty(bool makeDirty) => isDirty = makeDirty;
	public void invalidateLayout() { isLayoutDirty = true; isDirty = true; }
	internal void validateLayout() => isLayoutDirty = false;
}
