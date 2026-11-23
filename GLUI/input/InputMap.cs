using System.Numerics;
using Silk.NET.Input;

namespace GLUI.input;

public class InputMap {
	private readonly bool[] previousKeyStates = new bool[KeyIndexMap.totalUniqueKeys];
	private readonly bool[] currentKeyStates = new bool[KeyIndexMap.totalUniqueKeys];

	private readonly bool[] previousMouseButtonStates = new bool[MouseButtonIndexMap.totalUniqueButtons];
	private readonly bool[] currentMouseButtonStates = new bool[MouseButtonIndexMap.totalUniqueButtons];
	private readonly bool[] doubleClickedMouseButtonStates = new bool[MouseButtonIndexMap.totalUniqueButtons];
	private readonly double[] lastMouseButtonClickTimes = new double[MouseButtonIndexMap.totalUniqueButtons];
	private readonly Vector2[] lastMouseButtonClickPositions = new Vector2[MouseButtonIndexMap.totalUniqueButtons];

	private Vector2 previousMousePosition;
	private Vector2 currentMousePosition;
	private Vector2 scrollDelta;
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
		int index = KeyIndexMap.getIndex(key);
		return index >= 0 && currentKeyStates[index];
	}

	public bool isPressed(Key key) {
		int index = KeyIndexMap.getIndex(key);
		return index >= 0 && currentKeyStates[index] && !previousKeyStates[index];
	}

	public bool isReleased(Key key) {
		int index = KeyIndexMap.getIndex(key);
		return index >= 0 && !currentKeyStates[index] && previousKeyStates[index];
	}

	public Vector2 getMousePosition() => currentMousePosition;
	public Vector2 getMouseDelta() => currentMousePosition - previousMousePosition;
	public Vector2 getScrollDelta() => scrollDelta;

	public bool isHeld(MouseButton button) {
		int index = MouseButtonIndexMap.getIndex(button);
		return index >= 0 && currentMouseButtonStates[index];
	}

	public bool isPressed(MouseButton button) {
		int index = MouseButtonIndexMap.getIndex(button);
		return index >= 0 && currentMouseButtonStates[index] && !previousMouseButtonStates[index];
	}

	public bool isReleased(MouseButton button) {
		int index = MouseButtonIndexMap.getIndex(button);
		return index >= 0 && !currentMouseButtonStates[index] && previousMouseButtonStates[index];
	}

	public bool isDoubleClicked(MouseButton button) {
		int index = MouseButtonIndexMap.getIndex(button);
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
			currentMousePosition = mouse.Position;
			previousMousePosition = mouse.Position;
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
		scrollDelta = Vector2.Zero;

		if (inputContext == null)
			return;

		foreach (IKeyboard keyboard in inputContext.Keyboards) {
			foreach (Key key in keyboard.SupportedKeys) {
				int index = KeyIndexMap.getIndex(key);
				if (index >= 0 && currentKeyStates[index])
					onHeld(keyboard, key, (int)key);
			}
		}

		foreach (IMouse mouse in inputContext.Mice) {
			foreach (MouseButton button in mouse.SupportedButtons) {
				int index = MouseButtonIndexMap.getIndex(button);
				if (index >= 0 && currentMouseButtonStates[index])
					onMouseHeld(mouse, button);
			}
		}
	}

	private void onKeyDown(IKeyboard keyboard, Key key, int keyCode) {
		int index = KeyIndexMap.getIndex(key);
		if (index < 0)
			return;

		bool wasDown = currentKeyStates[index];
		currentKeyStates[index] = true;

		if (!wasDown)
			anyKeyPressedEvent?.Invoke(key);
	}

	private void onKeyUp(IKeyboard keyboard, Key key, int keyCode) {
		int index = KeyIndexMap.getIndex(key);
		if (index < 0)
			return;

		currentKeyStates[index] = false;
		anyKeyReleasedEvent?.Invoke(key);
	}

	private void onHeld(IKeyboard keyboard, Key key, int keyCode) => anyKeyHeldEvent?.Invoke(key);

	private void onMouseDown(IMouse mouse, MouseButton button) {
		int index = MouseButtonIndexMap.getIndex(button);
		if (index < 0)
			return;

		bool wasDown = currentMouseButtonStates[index];
		currentMouseButtonStates[index] = true;

		if (!wasDown) {
			double timeSinceLastClick = elapsedTime - lastMouseButtonClickTimes[index];
			float distanceFromLastClick = Vector2.Distance(currentMousePosition, lastMouseButtonClickPositions[index]);

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
	}

	private void onMouseUp(IMouse mouse, MouseButton button) {
		int index = MouseButtonIndexMap.getIndex(button);
		if (index < 0)
			return;

		currentMouseButtonStates[index] = false;
		anyMouseButtonReleasedEvent?.Invoke(button);
	}

	private void onMouseHeld(IMouse mouse, MouseButton button) => anyMouseButtonHeldEvent?.Invoke(button);
	private void onMouseMove(IMouse mouse, Vector2 position) => currentMousePosition = position;
	private void onMouseScroll(IMouse mouse, ScrollWheel wheel) => scrollDelta += new Vector2(wheel.X, wheel.Y);
}