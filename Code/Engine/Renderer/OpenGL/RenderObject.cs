namespace Engine.Renderer.OpenGL;
using Silk.NET.OpenGL;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
public class RenderObject : RenderState{
	public static Dictionary<string,RenderObject> all = [];
	public int vertexOffset;
	public int indexOffset;
	public int indirectOffset;
	public int[] uniformOffsets;
	public nuint[] uniformSizes;
	public int[] shaderStorageOffsets;
	public nuint[] shaderStorageSizes;
	public Material material;
	public Transform transform;
	public RenderObject(){
		this.uniformOffsets = new int[Globals.maxUniformBindings];
		this.uniformSizes = new nuint[Globals.maxUniformBindings];
		this.shaderStorageOffsets = new int[Globals.maxShaderStorageBindings];
		this.shaderStorageSizes = new nuint[Globals.maxShaderStorageBindings];
		this.shader = Shader.fallback;
		this.material = Material.fallback;
	}
	public static void Sort(){
		RenderObject.all = RenderObject.all.OrderBy(x=>x.Value.shader)
			.ThenBy(x=>x.Value.textures)
			.ThenBy(x=>x.Value.vertexArray)
			.ThenBy(x=>x.Value.shaderStorageBuffers)
			.ThenBy(x=>x.Value.uniformBuffers)
			.ThenBy(x=>x.Value.vertexBuffers)
			.ToDictionary();
	}
	public void ApplyStates(){
		var globalState = OpenGL.current.renderState;
		this.indirectBuffer?.BindIndirect();
		this.vertexArray?.Bind();
		this.vertexArray?.SetIndexBuffer(this.indexBuffer);
		for(var index = 0; index < this.vertexBuffers.Length; index += 1){
			this.vertexArray?.SetVertexBuffer(this.vertexBuffers[index],index,this.vertexOffset);
		}
		for(var index = 0; index < this.uniformBuffers.Length; index += 1){
			this.uniformBuffers[index]?.BindRange(true,this.uniformOffsets[index],this.uniformSizes[index],index);
		}
		for(var index = 0; index < this.shaderStorageBuffers.Length; index += 1){
			this.shaderStorageBuffers[index]?.BindRange(false,this.shaderStorageOffsets[index],this.shaderStorageSizes[index],index);
		}
		for(var index = 0; index < this.textures.Length; index += 1){
			if(globalState.textures[index] == this.textures[index]){continue;}
			this.textures[index]?.Bind((TextureUnit)index);
		}
		if(globalState.shader != this.shader){this.shader?.Use();}
	}
	public void ApplyMaterial(){
		foreach(var (name,value) in material.properties){
			var fullName = name.Contains('.') ? name : $"Default.{name}";
			var (blockName,uniformName) = (fullName[..fullName.LastIndexOf('.')], fullName[(fullName.IndexOf('.') + 1)..]);
			var stateBindings = default(Buffer[]);
			this.shader.uniformBlocks.TryGetValue(blockName,out var block);
			if(block is null){
				this.shader.shaderStorageBlocks.TryGetValue(blockName,out block);
				if(block is null){
					OpenGL.Log(GLEnum.DebugSourceApplication,GLEnum.DebugTypeError,GLEnum.DebugSeverityLow,$"Property '{fullName}' from material '{material.path}' doesn't exist in referenced shader pipeline.");
					continue;
				}
				stateBindings = this.shaderStorageBuffers;
			}
			stateBindings ??= this.uniformBuffers;
			block.fields.TryGetValue(uniformName,out var uniform);
			if(uniform is null){
				OpenGL.Log(GLEnum.DebugSourceApplication,GLEnum.DebugTypeError,GLEnum.DebugSeverityLow,$"Property '{uniformName}' from material '{material.path}' doesn't exist in block {blockName} of referenced shader pipeline.");
				continue;
			}
			var buffer = stateBindings[block.binding];
			void SetUniform<Type>(nint offset,Type value){
				var data = buffer.AsSpan<Type>(offset);
				if(!data[0].Equals(value)){data[0] = value;}
			}
			if(value is float floatValue){SetUniform((nint)uniform.offset,floatValue);}
			else if(value is int intValue){SetUniform((nint)uniform.offset,intValue);}
			else if(value is Vector4 vector4Value){SetUniform((nint)uniform.offset,vector4Value);}
			else if(value is Vector3 vector3Value){SetUniform((nint)uniform.offset,vector3Value);}
			else if(value is Vector2 vector2Value){SetUniform((nint)uniform.offset,vector2Value);}
			else if(value is Matrix4x4 matrix4x4Value){SetUniform((nint)uniform.offset,matrix4x4Value);}
			else if(value is bool boolValue){SetUniform((nint)uniform.offset,boolValue);}
		}
	}
}
public struct DrawElementsCommand{
	public uint indexCount;
	public uint instances;
	public uint firstIndex;
	public uint baseVertex;
	public uint baseInstance;
}