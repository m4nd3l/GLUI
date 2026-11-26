using System.Numerics;
using Silk.NET.Input;
using Silk.NET.Maths;

namespace GLUI.input;

public class InputMap {
	private readonly bool[] previousKeyStates = new bool[KeyIndexMap.totalUniqueKeys];
	private readonly bool[] currentKeyStates = new bool[KeyIndexMap.totalUniqueKeys];

	private readonly bool[] previousMouseButtonStates = new bool[MouseButtonIndexMap.totalUniqueButtons];
	private readonly bool[] currentMouseButtonStates = new bool[MouseButtonIndexMap.totalUniqueButtons];
	private readonly bool[] doubleClickedMouseButtonStates = new bool[MouseButtonIndexMap.totalUniqueButtons];
	private readonly double[] lastMouseButtonClickTimes = new double[MouseButtonIndexMap.totalUniqueButtons];
	private readonly Vector2D<float>[] lastMouseButtonClickPositions = new Vector2D<float>[MouseButtonIndexMap.totalUniqueButtons];

	private Vector2D<float> previousMousePosition;
	private Vector2D<float> currentMousePosition;
	private Vector2D<float> scrollDelta;
	private double elapsedTime;

	private IInputContext? inputContext;

	private double doubleClickIntervalSeconds { get; set; } = 0.30;
	private float doubleClickMaxDistance { get; set; } = 6.0f;

	public event Action<Key>? anyKeyPressedEvent;
	public event Action<Key>? anyKeyReleasedEvent;
	public event Action<Key>? anyKeyHeldEvent;

	public event Action<MouseButton>? anyMouseButtonPressedEvent;
	public event Action<MouseButton>? anyMouseButtonReleasedEvent;
	public event Action<MouseButton>? anyMouseButtonHeldEvent;
	public event Action<MouseButton>? anyMouseButtonDoubleClickedEvent;

	public bool isHeld(Key key) {
		var index = KeyIndexMap.getIndex(key);
		return index >= 0 && currentKeyStates[index];
	}

	public bool isPressed(Key key) {
		var index = KeyIndexMap.getIndex(key);
		return index >= 0 && currentKeyStates[index] && !previousKeyStates[index];
	}

	public bool isReleased(Key key) {
		var index = KeyIndexMap.getIndex(key);
		return index >= 0 && !currentKeyStates[index] && previousKeyStates[index];
	}

	public Vector2D<float> getMousePosition() => currentMousePosition;
	public Vector2D<float> getMouseDelta() => currentMousePosition - previousMousePosition;
	public Vector2D<float> getScrollDelta() => scrollDelta;

	public bool isHeld(MouseButton button) {
		var index = MouseButtonIndexMap.getIndex(button);
		return index >= 0 && currentMouseButtonStates[index];
	}

	public bool isPressed(MouseButton button) {
		var index = MouseButtonIndexMap.getIndex(button);
		return index >= 0 && currentMouseButtonStates[index] && !previousMouseButtonStates[index];
	}

	public bool isReleased(MouseButton button) {
		var index = MouseButtonIndexMap.getIndex(button);
		return index >= 0 && !currentMouseButtonStates[index] && previousMouseButtonStates[index];
	}

	public bool isDoubleClicked(MouseButton button) {
		var index = MouseButtonIndexMap.getIndex(button);
		return index >= 0 && doubleClickedMouseButtonStates[index];
	}

	public void attach(IInputContext context) {
		detach();
		inputContext = context;
		elapsedTime = 0.0;
		Array.Fill(lastMouseButtonClickTimes, double.NegativeInfinity);

		foreach (IKeyboard keyboard in inputContext.Keyboards) {
			keyboard.KeyDown += onKeyDown;
			keyboard.KeyUp += onKeyUp;
		}

		foreach (IMouse mouse in inputContext.Mice) {
			mouse.MouseDown += onMouseDown;
			mouse.MouseUp += onMouseUp;
			mouse.MouseMove += onMouseMove;
			mouse.Scroll += onMouseScroll;
			currentMousePosition = new Vector2D<float>(mouse.Position.X, mouse.Position.Y);
			previousMousePosition = new Vector2D<float>(mouse.Position.X, mouse.Position.Y);
		}
	}

	public void detach() {
		if (inputContext == null)
			return;

		foreach (IKeyboard keyboard in inputContext.Keyboards) {
			keyboard.KeyDown -= onKeyDown;
			keyboard.KeyUp -= onKeyUp;
		}

		foreach (IMouse mouse in inputContext.Mice) {
			mouse.MouseDown -= onMouseDown;
			mouse.MouseUp -= onMouseUp;
			mouse.MouseMove -= onMouseMove;
			mouse.Scroll -= onMouseScroll;
		}

		Array.Clear(doubleClickedMouseButtonStates);
		inputContext = null;
	}

	public void update(double deltaTime) {
		elapsedTime += Math.Max(0.0, deltaTime);

		Array.Copy(currentKeyStates, previousKeyStates, currentKeyStates.Length);
		Array.Copy(currentMouseButtonStates, previousMouseButtonStates, currentMouseButtonStates.Length);
		Array.Clear(doubleClickedMouseButtonStates);
		previousMousePosition = currentMousePosition;
		scrollDelta = Vector2D<float>.Zero;

		if (inputContext == null)
			return;

		foreach (IKeyboard keyboard in inputContext.Keyboards) {
			foreach (Key key in keyboard.SupportedKeys) {
				var index = KeyIndexMap.getIndex(key);
				if (index >= 0 && currentKeyStates[index])
					onHeld(keyboard, key, (int)key);
			}
		}

		foreach (IMouse mouse in inputContext.Mice) {
			foreach (MouseButton button in mouse.SupportedButtons) {
				var index = MouseButtonIndexMap.getIndex(button);
				if (index >= 0 && currentMouseButtonStates[index])
					onMouseHeld(mouse, button);
			}
		}
	}

	private void onKeyDown(IKeyboard keyboard, Key key, int keyCode) {
		var index = KeyIndexMap.getIndex(key);
		if (index < 0)
			return;

		var wasDown = currentKeyStates[index];
		currentKeyStates[index] = true;

		if (!wasDown)
			anyKeyPressedEvent?.Invoke(key);
	}

	private void onKeyUp(IKeyboard keyboard, Key key, int keyCode) {
		var index = KeyIndexMap.getIndex(key);
		if (index < 0)
			return;

		currentKeyStates[index] = false;
		anyKeyReleasedEvent?.Invoke(key);
	}

	private void onHeld(IKeyboard keyboard, Key key, int keyCode) => anyKeyHeldEvent?.Invoke(key);

	private void onMouseDown(IMouse mouse, MouseButton button) {
		var index = MouseButtonIndexMap.getIndex(button);
		if (index < 0)
			return;

		var wasDown = currentMouseButtonStates[index];
		currentMouseButtonStates[index] = true;

		if (wasDown) return;

		var timeSinceLastClick = elapsedTime - lastMouseButtonClickTimes[index];
		var distanceFromLastClick = Vector2.Distance(new Vector2(currentMousePosition.X, currentMousePosition.Y), new Vector2(lastMouseButtonClickPositions[index].X, lastMouseButtonClickPositions[index].Y));

		if (timeSinceLastClick >= 0.0 && timeSinceLastClick <= doubleClickIntervalSeconds && distanceFromLastClick <= doubleClickMaxDistance) {
			doubleClickedMouseButtonStates[index] = true;
			lastMouseButtonClickTimes[index] = double.NegativeInfinity;
			anyMouseButtonDoubleClickedEvent?.Invoke(button);
		}
		else {
			lastMouseButtonClickTimes[index] = elapsedTime;
			lastMouseButtonClickPositions[index] = currentMousePosition;
		}

		anyMouseButtonPressedEvent?.Invoke(button);
	}

	private void onMouseUp(IMouse mouse, MouseButton button) {
		var index = MouseButtonIndexMap.getIndex(button);
		if (index < 0) return;

		currentMouseButtonStates[index] = false;
		anyMouseButtonReleasedEvent?.Invoke(button);
	}

	private void onMouseHeld(IMouse mouse, MouseButton button) => anyMouseButtonHeldEvent?.Invoke(button);
	private void onMouseMove(IMouse mouse, Vector2 position) => currentMousePosition = new Vector2D<float>(position.X, position.Y);
	private void onMouseScroll(IMouse mouse, ScrollWheel wheel) => scrollDelta += new Vector2D<float>(wheel.X, wheel.Y);
}