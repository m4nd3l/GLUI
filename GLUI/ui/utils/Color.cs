namespace GLUI.ui.utils;

public class Color {
	public static Color black       { get; } = new(0f, 0f, 0f);
	public static Color white       { get; } = new();
	public static Color transparent { get; } = new(0f, 0f, 0f, 0f);
	public static Color red         { get; } = new(1f, 0f, 0f);
	public static Color green       { get; } = new(0f, 1f, 0f);
	public static Color blue        { get; } = new(0f, 0f, 1f);
	public static Color yellow      { get; } = new(1f, 1f, 0f);
	public static Color cyan        { get; } = new(0f);
	public static Color magenta     { get; } = new(1f, 0f);
	public static Color gray        { get; } = new(0.5f, 0.5f, 0.5f);

	public float r { get; set; }
	public float g { get; set; }
	public float b { get; set; }
	public float a { get; set; }

	public Color(Color color) : this(color.r, color.g, color.b, color.a) { }
	public Color(float r = 1f, float g = 1f, float b = 1f, float a = 1f) {
		this.r = r;
		this.g = g;
		this.b = b;
		this.a = a;
	}
}
