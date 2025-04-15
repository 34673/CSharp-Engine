namespace Engine.Renderer.OpenGL;
using Engine.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using Silk.NET.OpenGL;
public class Shader : IDisposable{
	public static Dictionary<string,Shader> all = [];
	public string name;
	public string path;
	public uint programHandle;
	public Dictionary<string,uint> stageHandles = [];
	public OpenGL context;
	static Shader(){
		var paths = Directory.EnumerateFiles(FileSystem.contentDirectory,"*.glsl",SearchOption.AllDirectories);
		foreach(var path in paths){
			var key = path.Split('/','\\').Last().Replace(".glsl","").Replace("@","");
			var shader = Shader.all[key] = new();
			shader.name = $"{key} Program";
			shader.path = path;
		}
	}
	public Shader(){}
	public static Shader TryLoad(string name){
		Shader.all.TryGetValue(name,out var shader);
		if(shader is null){return Shader.all["Default"];}
		if(shader.stageHandles.Count < 1){
			shader.context = OpenGL.current;
			shader.programHandle = shader.context.API.CreateProgram();
			OpenGL.current.API.ObjectLabel(GLEnum.Program,shader.programHandle,(uint)shader.name.Length,shader.name);
			shader.Parse();
			shader.Link();
			foreach(var stage in shader.stageHandles.Values){
				shader.context.API.DetachShader(shader.programHandle,stage);
				shader.context.API.DeleteShader(stage);
			}
		}
		return shader;
	}
	public void Parse(){
		var stageMap = new Dictionary<string,ShaderType>();
		var stageType = (ShaderType)(-1);
		var stageStart = -1;
		var stageBounds = (start:-1,end:-1);
		var stage = "";
		var name = "";
		stageMap["Vertex"] = ShaderType.VertexShader;
		stageMap["TessellationControl"] = ShaderType.TessControlShader;
		stageMap["Control"] = ShaderType.TessControlShader;
		stageMap["TessellationEvaluation"] = ShaderType.TessEvaluationShader;
		stageMap["Evaluation"] = ShaderType.TessEvaluationShader;
		stageMap["Geometry"] = ShaderType.GeometryShader;
		stageMap["Fragment"] = ShaderType.FragmentShader;
		stageMap["Pixel"] = ShaderType.FragmentShader;
		stageMap["Compute"] = ShaderType.ComputeShader;
		if(!File.Exists(this.path)){return;}
		var file = File.ReadAllLines(this.path);
		for(var line=0;line<file.Length;++line){
			var split = file[line].Split(" ");
			split = split.Select(x=>x.Trim()).ToArray();
			if(split.Length < 3){continue;}
			if(split[0] != "#define" || split[1] != "Stage"){continue;}
			if(stageStart > -1){
				stageBounds = (stageStart+1,line);
				this.stageHandles[stage] = this.Compile(stageType,string.Join("\n",file[stageBounds.start..stageBounds.end]));
				this.context.API.ObjectLabel(GLEnum.Shader,this.stageHandles[stage],(uint)name.Length,name);
			}
			stage = split[2];
			name = $"{this.name.Replace(" Program","")} {stage}";
			if(!stageMap.ContainsKey(stage)){continue;}
			stageType = stageMap[stage];
			stageStart = line;
		}
		stageBounds = (stageStart+1,file.Length);
		this.stageHandles[stage] = this.Compile(stageType,string.Join("\n",file[stageBounds.start..stageBounds.end]));
		this.context.API.ObjectLabel(GLEnum.Shader,this.stageHandles[stage],(uint)name.Length,name);
	}
	public uint Compile(ShaderType type,string source){
		var handle = this.context.API.CreateShader(type);
		this.context.API.ShaderSource(handle,source);
		this.context.API.CompileShader(handle);
		var logs = this.context.API.GetShaderInfoLog(handle);
		if(!string.IsNullOrWhiteSpace(logs)){
			throw new($"Error compiling shader of type {type}, failed with error {logs}");
		}
		return handle;
	}
	public void Link(){
		foreach(var stage in this.stageHandles.Values){
			this.context.API.AttachShader(this.programHandle,stage);
		}
		this.context.API.LinkProgram(this.programHandle);
		this.context.API.GetProgram(this.programHandle,GLEnum.LinkStatus,out var status);
		if(status == 0){
			throw new($"Program failed to link with error: {this.context.API.GetProgramInfoLog(this.programHandle)}");
		}
	}
	public unsafe void SetUniform<Type>(string name,Type value){
		var location = this.context.API.GetUniformLocation(this.programHandle,name);
		if(location == -1){throw new($"\"{name}\" uniform not found on shader \"{this.name}\"");}
		else if(value is int integer){this.context.API.Uniform1(location,integer);}
		else if(value is float floatingPoint){this.context.API.Uniform1(location,floatingPoint);}
		else if(value is Vector2 vector2){this.context.API.Uniform2(location,vector2.X,vector2.Y);}
		else if(value is Vector3 vector3){this.context.API.Uniform3(location,vector3.X,vector3.Y,vector3.Z);}
		else if(value is Vector4 vector4){this.context.API.Uniform4(location,vector4.X,vector4.Y,vector4.Z,vector4.W);}
		else if(value is Matrix4x4 matrix4x4){this.context.API.UniformMatrix4(location,1,false,(float*)&matrix4x4);}
		else{throw new($"Unsupported uniform type \"{value.GetType()}\" for property \"{name}\" in shader \"{this.name}\"");}
	}
	public void Use() => this.context.API.UseProgram(this.programHandle);
	public void Dispose() => this.context.API.DeleteProgram(this.programHandle);
}