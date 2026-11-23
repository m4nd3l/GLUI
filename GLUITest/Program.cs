using Silk.NET.Input;
using GLUI.window;

using GLWindow window = new GLWindow("GLUI Testing");
window.addKeyDownEvent((_, key, _) => { if (key == Key.Escape) window.close(); });
window.run();