using System.Linq;
using System.Numerics;
namespace Engine.Renderer;
public interface IRenderer{
	public void Start(){}
	public void Update(){}
	public void Update(double delta){}
	public void Stop(){}
	public void AddModel(string modelPath,string skinPath,Transform transform) => this.AddModel(Model.LoadFile(modelPath),Skin.LoadFile(skinPath),transform);
	public void AddModel(Model model,Skin skin,Transform transform){
		foreach(var mesh in model.submeshes.Values){
			var name = mesh.name.Split(":").Last();
			this.AddObject(mesh,skin[name],transform);
		}
	}
	public void AddObject(string meshName,string materialPath,Transform transform){
		Mesh.all.TryGetValue(meshName,out var mesh);
		this.AddObject(mesh,Material.TryLoad(materialPath),transform);
	}
	public void AddObject(Mesh mesh,string materialPath,Transform transform) => this.AddObject(mesh,Material.TryLoad(materialPath),transform);
	public void AddObject(Mesh mesh,Material material,Transform transform){}
	public void SetView(Transform transform,Matrix4x4 view,Matrix4x4 projection=default){}
	public void RemoveModel(string name){}
}