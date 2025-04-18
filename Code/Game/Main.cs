namespace Game;
using Engine.Core;
public class Main{
	public double delta;
	public Main(){
		Program.window.Load += this.Start;
		Program.window.Update += this.Update;
	}
	public void Start(){
		var root = FileSystem.contentDirectory;
		Program.frameTimer.Elapsed += (a,b)=>Program.window.Title += $" | Game: {(int)(1 / this.delta)} fps";
		SystemCalls.Renderer.AddModel(root+"/Characters/Test.fbx",root+"/Characters/Test.skin");
		var player = new Entity();
		var camera = new Camera();
	}
	public void Update(double delta){
		this.delta = delta;
	}
}