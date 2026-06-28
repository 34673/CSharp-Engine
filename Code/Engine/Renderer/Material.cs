namespace Engine.Renderer;
using System.Collections.Generic;
using System.IO;
using System.Globalization;
using System.Linq;
using System.Numerics;
public class Material{
	public static Dictionary<string,Material> all = [];
	public string name;
	public string path;
	public string shader;
	public Dictionary<string,object> properties = [];
	static Material(){
		var paths = Directory.EnumerateFiles(".","*.material",SearchOption.AllDirectories);
		foreach(var item in paths){
			var path = item.Replace("\\", "/");
			path = path[2..path.LastIndexOf('.')];
			var material = Material.all[path] = new();
			material.path = path;
			material.name = path.Substring(path.LastIndexOf('/') + 1);
		}
		var fallback = Material.all["Default"] = new();
		fallback.name = "Default";
		fallback.shader = "Default";
	}
	public Material(){}
	public static Material TryLoad(string path){
		Material.all.TryGetValue(path,out var material);
		if(material is null){return Material.all["Default"];}
		if(path != "Default" && material.properties.Count < 1){material.Load();}
		return material;
	}
	public static object ParseValue(string value){
		if(value.Contains('(')){
			var values = value.Trim('(',')').Split(',').Select(x=>x.Trim()).ToArray();
			if(values.Length is < 2 or > 4) {return default;}
			var floats = values.Select(x=>float.Parse(x,CultureInfo.InvariantCulture.NumberFormat)).ToArray();
			if(values.Length == 2){return new Vector2(floats[0],floats[1]);}
			if(values.Length == 3){return new Vector3(floats[0],floats[1],floats[2]);}
			if(values.Length == 4){return new Vector4(floats[0],floats[1],floats[2],floats[3]);}
		}
		return value.Contains('.') ? float.Parse(value) : int.Parse(value);
	}
	public void Load(){
		var path = this.path.TrimEnd('.');
		path = path.EndsWith(".material") ? path : $"{path}.material";
		if(!File.Exists(path)){return;}
		var scope = 0;
		foreach(var line in File.ReadAllLines(path)){
			var indented = line.StartsWith('\t') || line.StartsWith(' ');
			if(scope > 0 && !indented){break;}
			if(scope == 0){
				if(!line.Contains(':')){return;}
				this.shader = line.Replace(":","").Trim();
				scope += 1;
				continue;
			}
			var property = line.Trim('\t').Split("=").Select(x=>x.Trim());
			this.properties[property.First()] = Material.ParseValue(property.Last());
		}
	}
}