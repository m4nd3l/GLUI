using Silk.NET.Maths;

namespace GLUI.ui.utils;

public class Dimensions {
	public event Action? Changed;
	public Vector2D<float> size { get; private set; }
	public Vector2D<float> percentageToParent {
		get => percentageToParentReference;
		set {
			if (percentageToParentReference.Equals(value)) return;
			percentageToParentReference = value;
			calculateSize();
			Changed?.Invoke();
		}
	}
	public Vector2D<float> parent {
		get => parentReference;
		set {
			if (parentReference.Equals(value)) return;
			parentReference = value;
			setLayoutParent(parentReference);
			Changed?.Invoke();
		}
	}
	private Vector2D<float> percentageToParentReference;

	private Vector2D<float> parentReference;

	public Dimensions(Vector2D<float>? percentageToParent = null, Vector2D<float>? parent = null) {
		percentageToParent ??= new Vector2D<float>(30.0f);
		parent ??= new Vector2D<float>(1920.0f, 1080.0f);
		this.percentageToParent = (Vector2D<float>) percentageToParent;
		this.parent             = (Vector2D<float>) parent;
		calculateSize();
	}
	
	public Vector2D<float> calculateSize() => size = parent * percentageToParent / new Vector2D<float>(100.0f);
	
	internal void setLayoutParent(Vector2D<float> newParent) { parent = newParent; calculateSize(); }
	
}
