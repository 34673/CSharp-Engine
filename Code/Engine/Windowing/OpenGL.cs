using Silk.NET.Windowing;
namespace Engine.Windowing;
public class OpenGL{
	public IWindow window;
	public OpenGL(){
		var context = new GraphicsAPI();
		context.API = ContextAPI.OpenGL;
		context.Flags = ContextFlags.Debug;
		context.Profile = ContextProfile.Core;
		context.Version = new(4,6);
		var options = WindowOptions.Default;
		options.Size = new(1600,900);
		options.Title = "Test";
		options.VSync = false;
		options.FramesPerSecond = 10000000;
		options.API = context;
		this.window = Window.Create(options);
	}
}
