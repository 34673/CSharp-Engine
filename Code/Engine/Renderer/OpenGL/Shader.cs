namespace Engine.Renderer.OpenGL;
using Silk.NET.OpenGL;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
public class Shader : IDisposable{
	public static Dictionary<IEnumerable<string>,Shader> all = new(new PipelineComparer<string>());
	public static Dictionary<string,ShaderType> stages = new(){
		{"vertex",ShaderType.VertexShader},
		{"tessellationcontrol",ShaderType.TessControlShader},
		{"control",ShaderType.TessControlShader},
		{"tessellationevaluation",ShaderType.TessEvaluationShader},
		{"evaluation",ShaderType.TessEvaluationShader},
		{"geometry",ShaderType.GeometryShader},
		{"fragment",ShaderType.FragmentShader},
		{"pixel",ShaderType.FragmentShader},
		{"compute",ShaderType.ComputeShader}
	};
	public static Shader fallback;
	public uint programHandle;
	public Dictionary<ShaderType,uint> stageHandles = [];
	public OpenGL context;
	public static Shader TryLoad(Material material){
		var stages = new Dictionary<ShaderType,string>();
		foreach(var (stage,path) in material.shaderPipeline){
			var stageName = stage.ToLower();
			if(!Shader.stages.TryGetValue(stageName,out var stageType)){
				var listPrefix = $"{Environment.NewLine}\t- ";
				var message = $"Skipping unknown shader stage '{stageName}'. The following stages are supported:{listPrefix}";
				message += string.Join(listPrefix,Shader.stages.Keys.ToArray());
				OpenGL.Log(GLEnum.DebugSourceApplication,GLEnum.DebugTypeError,GLEnum.DebugSeverityMedium,message);
				continue;
			}
			if(!File.Exists(path)){
				OpenGL.Log(GLEnum.DebugSourceApplication,GLEnum.DebugTypeError,GLEnum.DebugSeverityMedium,$"Couldn't find file '{path}'. Skipping stage directive.");
				continue;
			}
			stages[stageType] = path;
		}
		if(!stages.ContainsKey(ShaderType.VertexShader) || !stages.ContainsKey(ShaderType.FragmentShader)){
			OpenGL.Log(GLEnum.DebugSourceApplication,GLEnum.DebugTypeError,GLEnum.DebugSeverityHigh,"Shader pipeline missing vertex or fragment stage.");
			throw new();
		}
		var pipeline = stages.Values;
		Shader.all.TryGetValue(pipeline,out var shader);
		if(shader is not null){return shader;}
		shader = new();
		shader.context = OpenGL.current;
		shader.programHandle = shader.context.API.CreateProgram();
		foreach(var (stageType,path) in stages){
			var name = $"{stageType} '{path}'";
			shader.stageHandles[stageType] = shader.Compile(stageType,File.ReadAllText(path));
			shader.context.API.ObjectLabel(GLEnum.Shader,shader.stageHandles[stageType],(uint)name.Length,name);
		}
		shader.Link();
		foreach(var stage in shader.stageHandles.Values){
			shader.context.API.DetachShader(shader.programHandle,stage);
			shader.context.API.DeleteShader(stage);
		}
		return Shader.all[pipeline] = shader;
	}
	public uint Compile(ShaderType type,string source){
		var handle = this.context.API.CreateShader(type);
		this.context.API.ShaderSource(handle,source);
		this.context.API.CompileShader(handle);
		var logs = this.context.API.GetShaderInfoLog(handle);
		if(!string.IsNullOrWhiteSpace(logs)){
			OpenGL.Log(GLEnum.DebugSourceShaderCompiler,GLEnum.DebugTypeError,GLEnum.DebugSeverityHigh,logs);
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
			var logs = this.context.API.GetProgramInfoLog(this.programHandle);
			if(!string.IsNullOrWhiteSpace(logs)){
				OpenGL.Log(GLEnum.DebugSourceShaderCompiler,GLEnum.DebugTypeError,GLEnum.DebugSeverityHigh,logs);
				return;
			}
		}
	}
	public unsafe void SetUniform<Type>(string name,Type value){
		var location = this.context.API.GetUniformLocation(this.programHandle,name);
		if(location == -1){
			OpenGL.Log(GLEnum.DebugSourceApplication,GLEnum.DebugTypeError,GLEnum.DebugSeverityHigh,$"Uniform \"{name}\" not found in bound shader");
			return;
		}
		else if(value is int integer){this.context.API.Uniform1(location,integer);}
		else if(value is float floatingPoint){this.context.API.Uniform1(location,floatingPoint);}
		else if(value is Vector2 vector2){this.context.API.Uniform2(location,vector2.X,vector2.Y);}
		else if(value is Vector3 vector3){this.context.API.Uniform3(location,vector3.X,vector3.Y,vector3.Z);}
		else if(value is Vector4 vector4){this.context.API.Uniform4(location,vector4.X,vector4.Y,vector4.Z,vector4.W);}
		else if(value is Matrix4x4 matrix4x4){this.context.API.UniformMatrix4(location,1,false,(float*)&matrix4x4);}
		else{throw new($"Unsupported uniform type \"{value.GetType()}\" for property \"{name}\" in bound shader");}
	}
	public void Use() => this.context.API.UseProgram(this.programHandle);
	public void Dispose() => this.context.API.DeleteProgram(this.programHandle);
	//Based on https://stackoverflow.com/a/14675741
	public class PipelineComparer<Type> : IEqualityComparer<IEnumerable<Type>>{
		public bool Equals(IEnumerable<Type> x, IEnumerable<Type> y) => Object.ReferenceEquals(x,y) || (x != null && y != null && x.SequenceEqual(y));
		public int GetHashCode(IEnumerable<Type> obj) => unchecked(obj.Where(e => e != null).Select(e => e.GetHashCode()).Aggregate(17,(a,b) => 23 * a + b));
	}
}