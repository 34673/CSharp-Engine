namespace Game;
using Engine.Core;
public class Main{
	public double delta;
	public Main(){
		Import.window.Load += this.Start;
		Import.window.Update += this.Update;
	}
	public void Start(){
		var root = FileSystem.contentDirectory;
		Program.frameTimer.Elapsed += (a,b)=>Import.window.Title += $" | Game: {(int)(1 / this.delta)} fps";
		var player = new Entity();
		var camera = new Camera();
		Import.renderer.AddModel(root+"/Characters/Test.fbx",root+"/Characters/Test.skin",player.transform);
	}
	public void Update(double delta){
		this.delta = delta;
	}
}