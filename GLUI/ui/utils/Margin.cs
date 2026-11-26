namespace GLUI.ui.utils;

public class Margin {
	private float leftMargin;
	private float topMargin;
	
	public Margin(float all) : this(all, all) {  }
	public Margin(float left = 0.0f, float top = 0.0f) {
		this.leftMargin = left;
		this.topMargin = top;
	}

	public float getLeftMargin() => leftMargin;
	public float getTopMargin() => topMargin;

	public void setLeftMargin(float newLeftMargin) => leftMargin = newLeftMargin;
	public void setTopMargin(float newTopMargin) => topMargin = newTopMargin; 
}
