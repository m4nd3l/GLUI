namespace GLUI.ui.utils;

public class Color {
	public static readonly Color black = new(0f, 0f, 0f);
	public static readonly Color white = new();
	public static readonly Color transparent = new (0.0f, 0.0f, 0.0f, 0.0f);
	public static readonly Color red = new (1.0f, 0.0f, 0.0f);
	public static readonly Color green = new(0.0f, 1.0f, 0.0f);
	public static readonly Color blue = new(0.0f, 0.0f, 1.0f);
	public static readonly Color yellow = new(1.0f, 1.0f, 0.0f);
	public static readonly Color cyan = new(0.0f);
	public static readonly Color magenta = new(1.0f, 0.0f);
	public static readonly Color gray = new(0.5f, 0.5f, 0.5f);
	
	private float rValue;
	private float gValue;
	private float bValue;
	private float aValue;
	
	public Color(Color color) : this(color.r(), color.g(), color.b(), color.a()) { }
	public Color(float r = 1.0f, float g = 1.0f, float b = 1.0f, float a = 1.0f) {
		rValue = r;
		gValue = g;
		bValue = b;
		aValue = a;
	}

	public float r() => rValue;
	public float g() => gValue;
	public float b() => bValue;
	public float a() => aValue;
	
	public void r(float r) => rValue = r;
	public void g(float g) => gValue = g;
	public void b(float b) => bValue = b;
	public void a(float a) => aValue = a;
}
