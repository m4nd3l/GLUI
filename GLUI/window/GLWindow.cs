using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using System.Numerics;
using GLUI.input;
using GLUI.rendering;
using GLUI.ui;
using GLUI.ui.utils;
using Texture = GLUI.rendering.opengl.Texture;

namespace GLUI.window;

public class GLWindow : IDisposable {
	private static readonly GraphicsAPI OpenGLGraphicsAPI = new(ContextAPI.OpenGL, ContextProfile.Core, ContextFlags.ForwardCompatible, new APIVersion(4, 6));

	private IWindow        window;
	private IInputContext? input;
	private InputMap?      inputMap;
	private GL?            gl;
	private GLUIRenderer?  renderer;
	private UI?            ui;
	private bool           running;
	private bool           disposed;
	
	private event Action?                    loadEvent;
	private event Action<InputMap?, double>? updateEvent;
	private event Action<double>?            renderEvent;
	private event Action<bool>?              focusChangedEvent;
	private event Action<Vector2D<int>>?     resizeEvent ;
	private event Action<Vector2D<int>>?     bufferResizeEvent;
	private event Action?                    closingEvent;

	private event Action<IKeyboard, Key, int> keyDownEvent;
	private event Action<IKeyboard, Key, int> keyUpEvent;
	private event Action<IKeyboard, char>     keyCharEvent;
	
	private event Action<IMouse, MouseButton>?          mouseDownEvent;
	private event Action<IMouse, MouseButton>?          mouseUpEvent;
	private event Action<IMouse, MouseButton, Vector2>? clickEvent;
	private event Action<IMouse, MouseButton, Vector2>? doubleClickEvent;
	private event Action<IMouse, Vector2>?              mouseMoveEvent;
	private event Action<IMouse, ScrollWheel>?          scrollEvent;
	
	public GLWindow(String title = "VulkanUI Window", int width = 1920, int height = 1080, int positionX = 250, int positionY = 250, bool vSync = true, int fps = 60, bool showNow = true) 
		: this(WindowOptions.Default with { Title = title, Size = new Vector2D<int>(width, height), Position = new Vector2D<int>(positionX, positionY), VSync = vSync, FramesPerSecond = fps, IsVisible = showNow, API = OpenGLGraphicsAPI }) { }

	public GLWindow(WindowOptions options) {
		running = false;
		
		window = Window.Create(options);
		
		window.Load              += load;
		window.Update            += deltaTime  => updateEvent?.Invoke(inputMap, deltaTime);
		window.Render            += deltaTime  => renderEvent?.Invoke(deltaTime);
		window.FocusChanged      += focused     => focusChangedEvent?.Invoke(focused);
		window.Resize            += size => resizeEvent?.Invoke(size);
		window.FramebufferResize += size => bufferResizeEvent?.Invoke(size);
		window.Closing           += ()               => closingEvent?.Invoke();
		
		addRenderEvent(render);
		addBufferResizeEvent(bufferResize);	
		addClosingEvent(onClosing);
	}

	private void load() {
		gl = window.CreateOpenGL();
		input = window.CreateInput();
		
		inputMap = new InputMap();
		inputMap.attach(input);
		
		window.Update += inputMap.update;

		foreach (var toAdd in input.Keyboards) {
			toAdd.KeyDown += (keyboard, key, keycode) => keyDownEvent?.Invoke(keyboard, key, keycode);
			toAdd.KeyUp   += (keyboard, key, keycode) => keyUpEvent?.Invoke(keyboard, key, keycode);
			toAdd.KeyChar += (keyboard, @char)        => keyCharEvent?.Invoke(keyboard, @char);
		}

		foreach (var toAdd in input.Mice) {
			toAdd.MouseDown   += (keyboard, key) => mouseDownEvent?.Invoke(keyboard, key);
			toAdd.MouseUp     += (keyboard, key) => mouseUpEvent?.Invoke(keyboard, key);
			toAdd.Click       += (mouse, button, position)         => clickEvent?.Invoke(mouse, button, position);
			toAdd.DoubleClick += (mouse, button, position)         => doubleClickEvent?.Invoke(mouse, button, position);
			toAdd.MouseMove   += (mouse, position)                 => mouseMoveEvent?.Invoke(mouse, position);
			toAdd.Scroll      += (mouse, scroll)                   => scrollEvent?.Invoke(mouse, scroll);
		}
		
		renderer = new GLUIRenderer(gl);
		renderer.updateProjection(window.Size.X, window.Size.Y);

		ui = new UI(renderer);
		ui.resize(window.Size.X, window.Size.Y);
		
		loadEvent?.Invoke();
	}
	
	private void render(double deltaTime) {
		if (gl is null) return;
		gl.ClearColor(0.1f, 0.1f, 0.15f, 1.0f);
		gl.Clear(ClearBufferMask.ColorBufferBit);
		renderer!.render();
	}

	private void onClosing() {
		inputMap?.detach();
		input?.Dispose();
		input = null;

		renderer?.Dispose();
		renderer = null;

		gl?.Dispose();
		gl = null;
	}

	private void bufferResize(Vector2D<int> size) {
		gl?.Viewport(size);
		renderer?.updateProjection(size.X, size.Y);
		ui?.resize(size.X, size.Y);
	}
	
	public void run() {
		if (running) return;
		running = true;
		window.Run();
		Dispose();
	}

	public void close() => window.Close();
	
	public bool isPressed(Key key) => getState(key) == KeyState.PRESSED;
	public bool isHeld(Key key) => getState(key) == KeyState.HELD;
	public bool isReleased(Key key) => getState(key) == KeyState.RELEASED;
	private KeyState getState(Key key) => inputMap!.isHeld(key) ? KeyState.HELD : 
		inputMap.isPressed(key) ? KeyState.PRESSED : KeyState.RELEASED;
	
	public bool isPressed(MouseButton key) => getState(key) == KeyState.PRESSED;
	public bool isHeld(MouseButton key) => getState(key) == KeyState.HELD;
	public bool isReleased(MouseButton key) => getState(key) == KeyState.RELEASED;
	public bool doubleClick(MouseButton key) => inputMap!.isDoubleClicked(key);
	private KeyState getState(MouseButton key) => inputMap!.isHeld(key) ? KeyState.HELD : 
		inputMap.isPressed(key) ? KeyState.PRESSED : KeyState.RELEASED;
	
	public void addKeyDownEvent(Action<IKeyboard, Key, int> handler)              => keyDownEvent += handler;
	public void addKeyUpEvent(Action<IKeyboard, Key, int> handler)                => keyUpEvent += handler;
	public void addKeyCharEvent(Action<IKeyboard, char> handler)                  => keyCharEvent += handler;
	public void addMouseDownEvent(Action<IMouse, MouseButton> handler)            => mouseDownEvent += handler;
	public void addMouseUpEvent(Action<IMouse, MouseButton> handler)              => mouseUpEvent += handler;
	public void addClickEvent(Action<IMouse, MouseButton, Vector2> handler)       => clickEvent += handler;
	public void addDoubleClickEvent(Action<IMouse, MouseButton, Vector2> handler) => doubleClickEvent += handler;
	public void addMouseMoveEvent(Action<IMouse, Vector2> handler)                => mouseMoveEvent += handler;
	public void addScrollEvent(Action<IMouse, ScrollWheel> handler)               => scrollEvent += handler;
	public void addLoadEvent(Action handler)                                      => loadEvent += handler;
	public void addUpdateEvent(Action<InputMap?, double> handler)                 => updateEvent += handler;
	public void addRenderEvent(Action<double> handler)                            => renderEvent += handler;
	public void addFocusChangedEvent(Action<bool> handler)                        => focusChangedEvent += handler;
	public void addResizeEvent(Action<Vector2D<int>> handler)                     => resizeEvent += handler;
	public void addBufferResizeEvent(Action<Vector2D<int>> handler)               => bufferResizeEvent += handler;
	public void addClosingEvent(Action handler)                                   => closingEvent += handler;
	
	public void removeKeyDownEvent(Action<IKeyboard, Key, int> handler)              => keyDownEvent -= handler;
	public void removeKeyUpEvent(Action<IKeyboard, Key, int> handler)                => keyUpEvent -= handler;
	public void removeKeyCharEvent(Action<IKeyboard, char> handler)                  => keyCharEvent -= handler;
	public void removeMouseDownEvent(Action<IMouse, MouseButton> handler)            => mouseDownEvent -= handler;
	public void removeMouseUpEvent(Action<IMouse, MouseButton> handler)              => mouseUpEvent -= handler;
	public void removeClickEvent(Action<IMouse, MouseButton, Vector2> handler)       => clickEvent -= handler;
	public void removeDoubleClickEvent(Action<IMouse, MouseButton, Vector2> handler) => doubleClickEvent -= handler;
	public void removeMouseMoveEvent(Action<IMouse, Vector2> handler)                => mouseMoveEvent -= handler;
	public void removeScrollEvent(Action<IMouse, ScrollWheel> handler)               => scrollEvent -= handler;
	public void removeLoadEvent(Action handler)                                      => loadEvent -= handler;
	public void removeUpdateEvent(Action<InputMap?, double> handler)                 => updateEvent -= handler;
	public void removeRenderEvent(Action<double> handler)                            => renderEvent -= handler;
	public void removeFocusChangedEvent(Action<bool> handler)                        => focusChangedEvent -= handler;
	public void removeResizeEvent(Action<Vector2D<int>> handler)                     => resizeEvent -= handler;
	public void removeBufferResizeEvent(Action<Vector2D<int>> handler)               => bufferResizeEvent -= handler;
	public void removeClosingEvent(Action handler)                                   => closingEvent -= handler;
	
	public void Dispose() {
		if (disposed) return;

		onClosing();
		window.Dispose();

		disposed = true;
		GC.SuppressFinalize(this);
	}
}
