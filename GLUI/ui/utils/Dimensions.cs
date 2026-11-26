using Silk.NET.Maths;

namespace GLUI.ui.utils;

public class Dimensions {
	private Vector2D<float> percentageToParent;
	private Vector2D<float> parent;
	private Vector2D<float> size;

	public Dimensions(Vector2D<float>? percentageToParent = null, Vector2D<float>? parent = null) {
		if (percentageToParent is null) percentageToParent = new Vector2D<float>(30.0f);
		if (parent is null)             parent = new Vector2D<float>(1920.0f, 1080.0f);
		this.percentageToParent = (Vector2D<float>) percentageToParent;
		this.parent             = (Vector2D<float>) parent;
		calculateSize();
	}
	
	public Vector2D<float> getSize() => size;
	public Vector2D<float> getParent() => parent;
	
	public void setPercentage(Vector2D<float> percentage) => percentageToParent = percentage;
	public void setParent(Vector2D<float> newParent) => parent = newParent;
	
	public Vector2D<float> calculateSize() => size = parent * percentageToParent / new Vector2D<float>(100.0f);
}
