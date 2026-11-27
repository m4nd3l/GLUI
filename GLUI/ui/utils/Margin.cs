using System;

namespace GLUI.ui.utils;

public class Margin {
	public event Action? Changed;
	public float leftMargin {
		get => leftMarginReference;
		set {
			if (leftMarginReference.Equals(value)) return;
			leftMarginReference = value;
			Changed?.Invoke();
		}
	}
	public float topMargin {
		get => topMarginReference;
		set {
			if (topMarginReference.Equals(value)) return;
			topMarginReference = value;
			Changed?.Invoke();
		}
	}
	private float leftMarginReference;
	private float topMarginReference;
	
	public Margin(float all) : this(all, all) {  }
	public Margin(float left = 0.0f, float top = 0.0f) {
		leftMargin = left;
		topMargin = top;
	}
}
