using Silk.NET.OpenGL;
using System;
using System.Runtime.InteropServices;
namespace Engine.Renderer.OpenGL{
	public unsafe class Buffer{
		public OpenGL context;
		public string name;
		public uint handle;
		public nint size;
		public nint pointer;
		public Span<Type> AsSpan<Type>() => new((void*)this.pointer,(int)this.size / Marshal.SizeOf<Type>());
		public nint Count<Type>() => this.size / 3 / Marshal.SizeOf<Type>();
		public Buffer(nint size,string label="Buffer"){
			var flags = GLEnum.MapWriteBit|GLEnum.MapPersistentBit|GLEnum.MapCoherentBit;
			this.context = OpenGL.current;
			this.name = label;
            this.handle = this.context.API.CreateBuffer();
			this.context.API.ObjectLabel(GLEnum.Buffer,this.handle,(uint)this.name.Length,this.name);
			this.size = size * 3;
			this.context.API.NamedBufferStorage(this.handle,(nuint)this.size,null,(uint)(flags|GLEnum.DynamicStorageBit));
			this.pointer = (nint)this.context.API.MapNamedBufferRange(this.handle,0,(nuint)this.size,(uint)flags);
		}
		public void BindIndirect(){
			if(this.context.renderState.indirectBuffer == this){return;}
			this.context.API.BindBuffer(GLEnum.DrawIndirectBuffer,this.handle);
			this.context.renderState.indirectBuffer = this;
		}
		public void BindRange(bool uniformBuffer,nint offset,nuint size,int bindingIndex=0){
			var globalState = this.context.renderState;
			var target = uniformBuffer ? GLEnum.UniformBuffer : GLEnum.ShaderStorageBuffer;
			this.context.API.BindBufferRange(target,(uint)bindingIndex,this.handle,offset,size);
			if(uniformBuffer && globalState.uniformBuffers[bindingIndex] != this){
				globalState.uniformBuffers[bindingIndex] = this;
			}
			else if(globalState.shaderStorageBuffers[bindingIndex] != this){
				globalState.shaderStorageBuffers[bindingIndex] = this;
			}
		}
		public void Dispose(){
			this.context.API.UnmapNamedBuffer(this.handle);
			this.context.API.DeleteBuffers(1,ref this.handle);
		}
	}
}
  