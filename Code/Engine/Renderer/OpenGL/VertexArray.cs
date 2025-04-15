namespace Engine.Renderer.OpenGL;
using Silk.NET.OpenGL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
public class VertexArray : IDisposable{
	public static Dictionary<string,VertexArray> all = [];
	public static Dictionary<VertexArray,List<VertexAttribute>> formats = [];
	public OpenGL context;
	public string name;
	public uint handle;
	public List<VertexAttribute> format = [];
	public int stride => this.format.Sum(x=>Marshal.SizeOf(x.type) * x.count);
	public uint attributeIndex;
	public uint attributeOffset;
	public Dictionary<Type,GLEnum> typeMap = new(){
		{typeof(int),GLEnum.Int},
		{typeof(float),GLEnum.Float},
		{typeof(double),GLEnum.Double},
		{typeof(Vector2),GLEnum.Float},
		{typeof(Vector3),GLEnum.Float},
		{typeof(Vector4),GLEnum.Float},
		{typeof(Matrix3x2),GLEnum.Float},
		{typeof(Matrix4x4),GLEnum.Float}
	};
	public VertexArray(string label="VertexArray"){
		this.context = OpenGL.current;
		this.handle = this.context.API.CreateVertexArray();
		this.name = label == "VertexArray" && VertexArray.all.Count > 0 ? label+VertexArray.all.Count + 1 : label;
		this.context.API.ObjectLabel(GLEnum.VertexArray,this.handle,(uint)this.name.Length,this.name);
		VertexArray.formats[this] = this.format;
	}
	public static VertexArray TryGet(IEnumerable<VertexAttribute> format){
		foreach(var vertexArray in VertexArray.all.Values){
			if(!format.SequenceEqual(vertexArray.format)){continue;}
			return vertexArray;
		}
		return null;
	}
	public void AddAttributes(IEnumerable<VertexAttribute> attributes){
		foreach(var attribute in attributes){
			this.AddAttribute(attribute);
		}
	}
	public void AddAttribute(VertexAttribute attribute){
		var glType = this.typeMap[attribute.type];
		this.format.Add(attribute);
		this.context.API.EnableVertexArrayAttrib(this.handle,this.attributeIndex);
		this.context.API.VertexArrayAttribBinding(this.handle,this.attributeIndex,0);
		if(glType == GLEnum.Int){
			this.context.API.VertexArrayAttribIFormat(this.handle,this.attributeIndex,attribute.count,glType,this.attributeOffset);
		}
		if(glType == GLEnum.Float){
			this.context.API.VertexArrayAttribFormat(this.handle,this.attributeIndex,attribute.count,glType,attribute.normalized,this.attributeOffset);
		}
		if(glType == GLEnum.Double){
			this.context.API.VertexArrayAttribLFormat(this.handle,this.attributeIndex,attribute.count,glType,this.attributeOffset);
		}
		this.attributeIndex += 1;
		this.attributeOffset += (uint)Marshal.SizeOf(attribute.type) * (uint)attribute.count;
	}
	public void Bind(){
		if(this.context.renderState.vertexArray == this){return;}
		this.context.API.BindVertexArray(this.handle);
		this.context.renderState.vertexArray = this;
	}
	public void SetVertexBuffer(Buffer buffer,int bindingIndex=0,int offset=0){
		if(this.format.Count < 1){throw new($"No format was provided for vertex array '{this.name}'. Can't determine vertex stride.");}
		var globalState = this.context.renderState.vertexBuffers[bindingIndex];
		if(offset == 0 && globalState == buffer){return;}
		this.context.API.VertexArrayVertexBuffer(this.handle,(uint)bindingIndex,buffer.handle,offset,(uint)this.stride);
		this.context.renderState.vertexBuffers[bindingIndex] = buffer;
	}
	public void SetIndexBuffer(Buffer buffer){
		if(this.context.renderState.indexBuffer == buffer){return;}
		this.context.API.VertexArrayElementBuffer(this.handle,buffer.handle);
		this.context.renderState.indexBuffer = buffer;
	}
	public void Dispose(){
		VertexArray.formats.Remove(this);
		this.context.API.DeleteVertexArray(this.handle);
	}
}