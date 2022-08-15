using Silk.NET.Input;
using Silk.NET.Windowing;
using System.Linq;
using SystemTimer = System.Timers.Timer;
namespace Engine.Core{
	public static class Program{
		public static IWindow window;
		public static SystemTimer frameTimer = new(1000);
		public static double delta;
		public static void Main(string[] items){
            var renderer = Loader.GetAssembly("Engine.Renderer.OpenGL");
            var game = Loader.GetAssembly("Game");
			var options = WindowOptions.Default;
			options.Size = new(1600,900);
			options.Title = "Test";
			options.VSync = false;
			options.FramesPerSecond = 5000;
			Program.window = Window.Create(options);
			Program.window.Load += Program.Start;
			Program.window.Update += Program.Update;
            var rendererInstance = Loader.Instantiate(renderer,"Engine.Renderer.OpenGL.OpenGL");
            Loader.Link(game,"Game.SystemCalls.Renderer",rendererInstance);
            var gameInstance = Loader.Instantiate(game,"Game.Main");
			Program.window.Run();
		}
		public static void Start(){
			Program.window.CreateInput().Keyboards.ToList().ForEach(x=>x.KeyDown += KeyDown);
			Program.frameTimer.AutoReset = true;
			Program.frameTimer.Elapsed += (a,b)=>Program.window.Title = "Core: "+((int)(1/Program.delta)).ToString()+" fps";
			Program.frameTimer.Start();
		}
		public static void Update(double delta){Program.delta = delta;}
		public static void KeyDown(IKeyboard keyboard,Key key,int arg){
			if(key == Key.Escape){window.Close();}
		}
	}
}
