namespace Engine.Renderer.OpenGL;
using shaderc;
using Silk.NET.Core.Native;
using Silk.NET.OpenGL;
using Silk.NET.SPIRV.Reflect;
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
	public static Dictionary<ShaderType,ShaderKind> glToShadercStages = new(){
		{ShaderType.VertexShader,ShaderKind.VertexShader},
		{ShaderType.TessControlShader,ShaderKind.TessControlShader},
		{ShaderType.TessEvaluationShader,ShaderKind.TessEvaluationShader },
		{ShaderType.GeometryShader,ShaderKind.GeometryShader},
		{ShaderType.FragmentShader,ShaderKind.FragmentShader},
		{ShaderType.ComputeShader,ShaderKind.ComputeShader}
	};
	public static Shader fallback;
	public static Compiler compiler = new();
	public static Reflect reflection = Reflect.GetApi();
	public uint programHandle;
	public Dictionary<ShaderType,uint> stageHandles = [];
	public Dictionary<string,InterfaceBlockLayout> uniformBlocks = [];
	public Dictionary<string,InterfaceBlockLayout> shaderStorageBlocks = [];
	public OpenGL context;
	static Shader(){
		Shader.compiler.Options.SetTargetEnvironment(TargetEnvironment.OpenGL,0);
		Shader.compiler.Options.IncludeDirectories.Add(Environment.CurrentDirectory);
		Shader.compiler.Options.EnableDebugInfo();
		Shader.compiler.Options.AutoMapLocations = true;
		Shader.compiler.Options.Optimization = OptimizationLevel.Performance;
		Shader.compiler.Options.TargetSpirVVersion = new(1,0);
	}
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
		var pipeline = stages.Values.ToArray();
		Shader.all.TryGetValue(pipeline,out var shader);
		if(shader is not null){return shader;}
		shader = new();
		shader.context = OpenGL.current;
		shader.programHandle = shader.context.API.CreateProgram();
		foreach(var (stageType,path) in stages){
			var name = $"{stageType} '{path}'";
			shader.stageHandles[stageType] = shader.Compile(stageType,path);
			shader.context.API.ObjectLabel(GLEnum.Shader,shader.stageHandles[stageType],(uint)name.Length,name);
		}
		shader.Link();
		foreach(var stage in shader.stageHandles.Values){
			shader.context.API.DetachShader(shader.programHandle,stage);
			shader.context.API.DeleteShader(stage);
		}
		return Shader.all[pipeline] = shader;
	}
	public unsafe uint Compile(ShaderType stage,string path){
		var source = File.ReadAllText(path);
		var result = Shader.compiler.Compile(source,path,Shader.glToShadercStages[stage]);
		var messageType = result.Status == Status.Success ? GLEnum.DontCare : GLEnum.DebugTypeError;
		var severity = result.Status == Status.Success ? GLEnum.DebugSeverityMedium : GLEnum.DebugSeverityHigh;
		if(!string.IsNullOrEmpty(result.ErrorMessage)){
			OpenGL.Log(GLEnum.DebugSourceShaderCompiler,messageType,severity,result.ErrorMessage);
		}
		var handle = this.context.API.CreateShader(stage);
		var binary = new Span<byte>((void*)result.CodePointer,(int)result.CodeLength);
		this.context.API.ShaderBinary<byte>(new Span<uint>([handle]),ShaderBinaryFormat.ShaderBinaryFormatSpirV,binary);
		this.context.API.SpecializeShader(handle,"main",0,null,null);
		var module = new ReflectShaderModule();
		Shader.reflection.CreateShaderModule(result.CodeLength,(void*)result.CodePointer,ref module);
		this.ReflectLayouts(ref module);
		return handle;
	}
	public unsafe void ReflectLayouts(ref ReflectShaderModule module){
		var descriptorBindings = new Span<DescriptorBinding>(module.DescriptorBindings,(int)module.DescriptorBindingCount);
		foreach(var descriptorBinding in descriptorBindings){
			var spirvBlock = descriptorBinding.Block;
			var blockName = SilkMarshal.PtrToString((nint)spirvBlock.Name);
			var isUniformBlock = descriptorBinding.DescriptorType == DescriptorType.UniformBuffer;
			var blocks = isUniformBlock ? this.uniformBlocks : this.shaderStorageBlocks;
			var block = blocks[blockName] = new();
			block.size = spirvBlock.PaddedSize;
			block.isUniformBlock = isUniformBlock;
			block.binding = descriptorBinding.Binding;
			var members = new Span<BlockVariable>(spirvBlock.Members,(int)spirvBlock.MemberCount);
			this.ReflectFields(members,block.fields);
		}
	}
	public unsafe void ReflectFields(Span<BlockVariable> members,Dictionary<string,ReflectionData> fields){
		foreach(var member in members){
			var memberName = SilkMarshal.PtrToString((nint)member.Name);
			var type = (TypeFlagBits)member.TypeDescription->TypeFlags;
			var field = fields[memberName] = new();
			field.offset = member.Offset;
			if(type.HasFlag(TypeFlagBits.Array)){
				field.arrayLengths = new Span<uint>(member.Array.Dims,(int)member.Array.DimsCount).ToArray().ToList();
			}
			if(type.HasFlag(TypeFlagBits.Struct)){
				var nestedMembers = new Span<BlockVariable>(member.Members,(int)member.MemberCount);
				this.ReflectFields(nestedMembers,field.children);
			}
			else if(type.HasFlag(TypeFlagBits.Vector)){
				field.components = member.Numeric.Vector.ComponentCount;
			}
			else if(type.HasFlag(TypeFlagBits.Matrix)){
				var columns = member.Numeric.Matrix.ColumnCount == 3 ? 4 : member.Numeric.Matrix.ColumnCount;
				field.arrayLengths.Add(columns);
			}
			if(type.HasFlag(TypeFlagBits.Float)){field.baseType = member.Numeric.Scalar.Width == 64 ? typeof(double) : typeof(float);}
			else if(type.HasFlag(TypeFlagBits.Int)){field.baseType = member.Numeric.Scalar.Signedness == 0 ? typeof(uint) : typeof(int);}
			else if(type.HasFlag(TypeFlagBits.Bool)){field.baseType = typeof(int);}
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