namespace Engine.Renderer;
using System.Collections.Generic;
using System.IO;
using System.Linq;
public class Skin : Dictionary<string,string>{
	public static Dictionary<string,Skin> all = [];
	public string path;
	public string name;
	public static Skin LoadFile(string path){
		var skin = new Skin();
		skin.path = path = path.Replace('\\','/');
		if(!path.EndsWith(".skin") || !File.Exists(path)){return skin;}
		skin.path = path;
		skin.name = path[(path.LastIndexOf('/') + 1)..path.LastIndexOf('.')];
		foreach(var line in File.ReadAllLines(path)){
			var split = line.Split(",").Select(x=>x.Trim()).ToArray();
			if(split[0] == ""){continue;}
			skin[split[0]] = split[1] == "" ? "Default" : split[1];
		}
		return Skin.all[skin.path] = skin;
	}
	public void ApplyTo(string modelPath){
		Model.all.TryGetValue(modelPath,out var model);
		if(model is null){return;}
		this.ApplyTo(model);
	}
	public void ApplyTo(Model model){
		foreach(var (submesh,material) in this){
			if(!model.submeshes.ContainsKey(submesh)){continue;}
			model.materials[submesh] = Material.TryLoad(material);
		}
	}
}