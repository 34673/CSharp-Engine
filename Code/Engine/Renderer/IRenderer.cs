using System.Numerics;

namespace Engine.Renderer;
public interface IRenderer{
	public void Start(){}
	public void Update(){}
	public void Update(double delta){}
	public void Stop(){}
	public void AddModel(string modelPath,string skinPath,Transform transform){}
	public void AddModel(Model model,Skin skin,Transform transform){}
	public void SetView(Transform transform,Matrix4x4 view,Matrix4x4 projection=default){}
	public void RemoveModel(string name){}
}