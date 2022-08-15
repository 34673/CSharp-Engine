using System.Collections.Generic;
using System.IO;
using System.Linq;
namespace Engine.Renderer{
	public class Skin : Dictionary<string,string>{
        public static Dictionary<string,Skin> all = new();
        public string path;
		public Skin(){}
        public Skin(string filePath) => Skin.LoadFile(filePath);
		public static Skin LoadFile(string path){
			var skin = new Skin();
			skin.path = path = path.Replace('\\','/');
			path = path.TrimEnd('.');
			path = path.EndsWith(".skin") ? path : path+".skin";
			if(!File.Exists(path)){return new();}
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
}
