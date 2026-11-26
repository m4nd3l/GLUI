namespace GLUI.ui.utils;

public class UIState {
	private bool isDirty;

	public UIState() => dirty(true);
	
	public void dirty(bool makeDirty) => isDirty = makeDirty;
}
