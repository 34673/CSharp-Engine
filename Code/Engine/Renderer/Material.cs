namespace Engine.Renderer;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
public class Material{
	public static Dictionary<string,Material> all = [];
	public static Material fallback;
	public string name;
	public string path;
	public Dictionary<string,string> shaderPipeline = [];
	public Dictionary<string,object> properties = [];
	public static Material TryLoad(string path){
		Material.all.TryGetValue(path,out var material);
		if(material is not null){return material;}
		material = new();
		material.path = path;
		material.Load();
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
		if(string.IsNullOrEmpty(this.name)){
			this.path = this.path.Replace("\\", "/");
			this.name = this.path.Substring(this.path.LastIndexOf('/') + 1);
		}
		var path = this.path.TrimEnd('.');
		path = path.EndsWith(".material") ? path : $"{path}.material";
		if(!File.Exists(path)){return;}
		foreach(var line in File.ReadAllLines(path)){
			if(line.TrimStart().StartsWith("//")){continue;}
			var tokens = line.Trim('\t').Split(' ').Select(x=>x.Trim()).ToArray();
			if(tokens[0] == "Stage"){
				if(tokens.Length < 3){throw new("[Material.Load()] Expected usage for Stage keyword: 'Stage <stageName> <pathToShaderFile>'");}
				this.shaderPipeline[tokens[1]] = tokens[2].Replace("\"","");
				continue;
			}
			var property = line.Trim('\t').Split("=").Select(x=>x.Trim());
			this.properties[property.First()] = Material.ParseValue(property.Last());
		}
		this.shaderPipeline = this.shaderPipeline.OrderBy(x=>x.Key).ToDictionary();
	}
}