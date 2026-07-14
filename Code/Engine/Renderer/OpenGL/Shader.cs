namespace Engine.Renderer.OpenGL;
using Silk.NET.OpenGL;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
	public Dictionary<string,InterfaceBlockLayout> uniformBlocks = [];
	public Dictionary<string,InterfaceBlockLayout> shaderStorageBlocks = [];
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
	public unsafe void ReflectLayouts(){
		var blockProperties = new[]{ProgramResourceProperty.NumActiveVariables,ProgramResourceProperty.NameLength,ProgramResourceProperty.BufferBinding,ProgramResourceProperty.BufferDataSize}; 
		var activeProperties = new[]{ProgramResourceProperty.ActiveVariables};
		var ReflectBlock = (ProgramInterface interfaceType) => {
			this.context.API.GetProgramInterface(this.programHandle,interfaceType,ProgramInterfacePName.ActiveResources,out var blockCount);
			var blockInfo = new int[blockProperties.Length];
			var blocks = interfaceType == ProgramInterface.UniformBlock ? this.uniformBlocks : this.shaderStorageBlocks;
			for(var blockIndex=0u;blockIndex<blockCount;blockIndex+=1){
				this.context.API.GetProgramResource(this.programHandle,interfaceType,blockIndex,blockProperties,null,blockInfo);
				this.context.API.GetProgramResourceName(this.programHandle,interfaceType,blockIndex,(uint)blockInfo[1],out _,out string blockName);
				if(blockInfo[0] < 1){continue;}
				var block = blocks[blockName] = new();
				block.binding = (uint)blockInfo[2];
				block.size = (uint)blockInfo[3];
				block.isUniformBlock = interfaceType == ProgramInterface.UniformBlock;
				var uniforms = new int[blockInfo[0]];
				this.context.API.GetProgramResource(this.programHandle,interfaceType,blockIndex,activeProperties,null,uniforms);
				this.ReflectFields(uniforms,block.fields);
			}
		};
		ReflectBlock(ProgramInterface.UniformBlock);
		ReflectBlock(ProgramInterface.ShaderStorageBlock);
	}
	public unsafe void ReflectFields(Span<int> members,Dictionary<string,ReflectionData> fields){
		var uniformProperties = new[]{
			ProgramResourceProperty.NameLength,
			ProgramResourceProperty.Type,
			ProgramResourceProperty.Offset,
			ProgramResourceProperty.ArraySize,
			ProgramResourceProperty.ArrayStride,
			ProgramResourceProperty.MatrixStride,
			ProgramResourceProperty.IsRowMajor
		};
		foreach(var uniformIndex in members){
			var uniformInfo = new int[uniformProperties.Length];
			this.context.API.GetProgramResource(this.programHandle,ProgramInterface.Uniform,(uint)uniformIndex,uniformProperties,null,uniformInfo);
			this.context.API.GetProgramResourceName(this.programHandle,ProgramInterface.Uniform,(uint)uniformIndex,(uint)uniformInfo[1],out _,out string uniformName);
			var uniform = fields[uniformName] = new();
			var type = (UniformType)uniformInfo[1];
			if(type == UniformType.Int){uniform.baseType = typeof(int);}
			else if(type == UniformType.UnsignedInt){uniform.baseType = typeof(uint);}
			else if(type == UniformType.Float){uniform.baseType = typeof(float);}
			else if(type == UniformType.Double){uniform.baseType = typeof(double);}
			else if(type == UniformType.Bool){uniform.baseType = typeof(bool);}
			uniform.offset = (uint)uniformInfo[2];
			uniform.arrayLengths.Add((uint)uniformInfo[3]);
			if(type is UniformType.BoolVec2 or UniformType.DoubleVec2 or UniformType.FloatVec2 or UniformType.IntVec2 or UniformType.UnsignedIntVec2){uniform.components = 2;}
			else if(type is UniformType.BoolVec3 or UniformType.DoubleVec3 or UniformType.FloatVec3 or UniformType.IntVec3 or UniformType.UnsignedIntVec3){uniform.components = 4;}
			else if(type is UniformType.BoolVec4 or UniformType.DoubleVec4 or UniformType.FloatVec4 or UniformType.IntVec4 or UniformType.UnsignedIntVec4){uniform.components = 4;}
			else if(type is UniformType.DoubleMat2 or UniformType.DoubleMat2x3 or UniformType.DoubleMat2x4){uniform.components = 2;}
			else if(type is UniformType.DoubleMat3 or UniformType.DoubleMat3x2 or UniformType.DoubleMat3x4){uniform.components = 4;}
			else if(type is UniformType.DoubleMat4 or UniformType.DoubleMat4x2 or UniformType.DoubleMat4x3){uniform.components = 4;}
			else if(type is UniformType.FloatMat2 or UniformType.FloatMat2x3 or UniformType.FloatMat2x4){uniform.components = 2;}
			else if(type is UniformType.FloatMat3 or UniformType.FloatMat3x2 or UniformType.FloatMat3x4){uniform.components = 4;}
			else if(type is UniformType.FloatMat4 or UniformType.FloatMat4x2 or UniformType.FloatMat4x3){uniform.components = 4;}
		}
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
		this.ReflectLayouts();
	}
	public void Use() => this.context.API.UseProgram(this.programHandle);
	public void Dispose() => this.context.API.DeleteProgram(this.programHandle);
	//Based on https://stackoverflow.com/a/14675741
	public class PipelineComparer<Type> : IEqualityComparer<IEnumerable<Type>>{
		public bool Equals(IEnumerable<Type> x,IEnumerable<Type> y) => Object.ReferenceEquals(x,y) || (x != null && y != null && x.SequenceEqual(y));
		public int GetHashCode(IEnumerable<Type> obj) => unchecked(obj.Where(e => e != null).Select(e => e.GetHashCode()).Aggregate(17,(a,b) => 23 * a + b));
	}
}
public class ReflectionData{
	public Type baseType;
	public uint offset;
	public List<uint> arrayLengths = [];
	public uint components = 1;
	public Dictionary<string,ReflectionData> children = [];
}
public class InterfaceBlockLayout{
	public uint binding;
	public uint size;
	public bool isUniformBlock;
	public Dictionary<string,ReflectionData> fields = [];
}