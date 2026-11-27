using Silk.NET.Maths;

namespace GLUI.ui.utils;

public class Alignment {
	public static readonly Alignment TOP_LEFT = new (0.0f, 0.0f);
	public static readonly Alignment TOP_CENTER = new (0.5f, 0.0f);
	public static readonly Alignment TOP_RIGHT = new (1.0f, 0.0f);

	public static readonly Alignment CENTER_LEFT = new (0.0f, 0.5f);
	public static readonly Alignment CENTER = new (0.5f, 0.5f);
	public static readonly Alignment CENTER_RIGHT = new (1.0f, 0.5f);

	public static readonly Alignment BOTTOM_LEFT = new (0.0f, 1.0f);
	public static readonly Alignment BOTTOM_CENTER = new (0.5f, 1.0f);
	public static readonly Alignment BOTTOM_RIGHT = new (1.0f, 1.0f);

	private float xRatio { get; }
	private float yRatio { get; }

	public Alignment(float xRatio, float yRatio) {
		this.xRatio = xRatio;
		this.yRatio = yRatio;
	}

	public Vector2D<float> getOffset(Dimensions dimensions) => getOffset(dimensions, new Margin(0));

	public Vector2D<float> getOffset(Dimensions dimensions, Margin margin) {
		var parentSize = dimensions.parent;
		var compSize = dimensions.size;

		float x = parentSize.X * xRatio - compSize.X * xRatio + margin.leftMargin;
		float y = parentSize.Y * yRatio - compSize.Y * yRatio + margin.topMargin;

		return new Vector2D<float>(x, y);
	}
}
