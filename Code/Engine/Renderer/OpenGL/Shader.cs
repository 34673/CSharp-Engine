using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using Silk.NET.OpenGL;
namespace Engine.Renderer.OpenGL{
    using Engine.Core;
	public class Shader : IDisposable{
        public static Dictionary<string,Shader> all = new();
		public string name;
        public string path;
		public uint programHandle;
        public Dictionary<string,uint> stageHandles = new();
		public GL renderer;
        static Shader(){
            var paths = Directory.EnumerateFiles(FileSystem.contentDirectory,"*.glsl",SearchOption.AllDirectories);
            foreach(var path in paths){
                var key = path.Split('/','\\').Last().Replace(".glsl","").Replace("@","");
                var shader = Shader.all[key] = new();
                shader.name = key;
                shader.path = path;
            }
        }
        public Shader(){}
        public static Shader TryLoad(string name){
            if(!Shader.all.ContainsKey(name)){return Shader.all["Default"];}
            var shader = Shader.all[name];
            if(shader.stageHandles.Count < 1){
                shader.renderer = OpenGL.current.API;
                shader.programHandle = shader.renderer.CreateProgram();
                shader.Parse();
                shader.Link();
                foreach(var stage in shader.stageHandles.Values){
                    shader.renderer.DetachShader(shader.programHandle,stage);
                    shader.renderer.DeleteShader(stage);
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
                    this.stageHandles[stage] = this.Compile(stageType,String.Join("\n",file[stageBounds.start..stageBounds.end]));
                }
                stage = split[2];
                if(!stageMap.ContainsKey(stage)){continue;}
                stageType = stageMap[stage];
                stageStart = line;
            }
            stageBounds = (stageStart+1,file.Length);
            this.stageHandles[stage] = this.Compile(stageType,String.Join("\n",file[stageBounds.start..stageBounds.end]));
        }
        public uint Compile(ShaderType type,string source){
			var handle = this.renderer.CreateShader(type);
			this.renderer.ShaderSource(handle,source);
			this.renderer.CompileShader(handle);
			var logs = this.renderer.GetShaderInfoLog(handle);
			if(!string.IsNullOrWhiteSpace(logs)){
				throw new($"Error compiling shader of type {type}, failed with error {logs}");
			}
			return handle;
        }
        public void Link(){
            foreach(var stage in this.stageHandles.Values){
                this.renderer.AttachShader(this.programHandle,stage);
            }
            this.renderer.LinkProgram(this.programHandle);
			this.renderer.GetProgram(this.programHandle,GLEnum.LinkStatus,out var status);
			if(status == 0){
                throw new($"Program failed to link with error: {this.renderer.GetProgramInfoLog(programHandle)}");
            }
        }
		public void SetUniform<Type>(string name,Type value){
			var location = this.renderer.GetUniformLocation(this.programHandle,name);
			if(location == -1){throw new($"\"{name}\" uniform not found on shader \"{this.name}\"");}
			else if(value is int integer){this.renderer.Uniform1(location,integer);}
			else if(value is float floatingPoint){this.renderer.Uniform1(location,floatingPoint);}
			else if(value is Vector2 vector2){this.renderer.Uniform2(location,vector2.X,vector2.Y);}
			else if(value is Vector3 vector3){this.renderer.Uniform3(location,vector3.X,vector3.Y,vector3.Z);}
			else if(value is Vector4 vector4){this.renderer.Uniform4(location,vector4.X,vector4.Y,vector4.Z,vector4.W);}
			else if(value is Matrix4x4 matrix4x4){
                unsafe{this.renderer.UniformMatrix4(location,1,false,(float*)&matrix4x4);}
            }
            else{throw new($"Unsupported uniform type \"{value.GetType()}\" for property \"{name}\" in shader \"{this.name}\"");}
		}
		public void Use() => this.renderer.UseProgram(this.programHandle);
		public void Dispose() => this.renderer.DeleteProgram(this.programHandle);
	}
}
