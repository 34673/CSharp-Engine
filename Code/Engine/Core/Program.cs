namespace Engine.Core;
using Engine.Windowing;
using Silk.NET.Input;
using Silk.NET.Windowing;
using System.Linq;
using SystemTimer = System.Timers.Timer;
public static class Program{
	public static SystemTimer frameTimer = new(1000);
	public static double delta;
	public static void Main(string[] items){
		FileSystem.Start();
		var windowing = Loader.GetAssembly("Engine.Windowing");
		var renderer = Loader.GetAssembly("Engine.Renderer.OpenGL");
		var game = Loader.GetAssembly("Game");
		Import.window = new OpenGL().window;
		Import.window.Load += Program.Start;
		Import.window.Update += Program.Update;
		Loader.Link(renderer,"Engine.Renderer.OpenGL.Import.window",Import.window);
		var rendererInstance = Loader.Instantiate(renderer,"Engine.Renderer.OpenGL.OpenGL");
		Loader.Link(game,"Game.Import.window",Import.window);
		Loader.Link(game,"Game.Import.renderer",rendererInstance);
		Loader.Instantiate(game,"Game.Main");
		Import.window.Run();
	}
	public static void Start(){
		Import.window.CreateInput().Keyboards.ToList().ForEach(x=>x.KeyDown += KeyDown);
		Program.frameTimer.AutoReset = true;
		Program.frameTimer.Elapsed += (a,b)=>Import.window.Title = $"Core: {(int)(1 / Program.delta)} fps";
		Program.frameTimer.Start();
	}
	public static void Update(double delta) => Program.delta = delta;
	public static void KeyDown(IKeyboard keyboard,Key key,int arg){
		if(key == Key.Escape){Import.window.Close();}
	}
}