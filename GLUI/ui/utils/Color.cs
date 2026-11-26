namespace GLUI.ui.utils;

public class Color {
	public static readonly Color black = new(0f, 0f, 0f);
	public static readonly Color white = new(1f, 1f, 1f);
	public static readonly Color transparent = new(0f, 0f, 0f, 0f);
	public static readonly Color red = new(1f, 0f, 0f);
	public static readonly Color green = new(0f, 1f, 0f);
	public static readonly Color blue = new(0f, 0f, 1f);
	public static readonly Color yellow = new(1f, 1f, 0f);
	public static readonly Color cyan = new(0f, 1f, 1f);
	public static readonly Color magenta = new(1f, 0f, 1f);
	public static readonly Color gray = new(0.5f, 0.5f, 0.5f);

	private float rValue, gValue, bValue, aValue;

	public Color(Color color) : this(color.r(), color.g(), color.b(), color.a()) { }
	public Color(float r = 1f, float g = 1f, float b = 1f, float a = 1f) {
		rValue = r; gValue = g; bValue = b; aValue = a;
	}
	public float r() => rValue;
	public float g() => gValue;
	public float b() => bValue;
	public float a() => aValue;
	public void r(float value) => rValue = value;
	public void g(float value) => gValue = value;
	public void b(float value) => bValue = value;
	public void a(float value) => aValue = value;
}
