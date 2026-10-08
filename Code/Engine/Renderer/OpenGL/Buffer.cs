namespace Engine.Renderer.OpenGL;
using Silk.NET.OpenGL;
using System;
using System.Runtime.InteropServices;
public unsafe class Buffer{
	public OpenGL context;
	public string name;
	public uint handle;
	public nint size;
	public nint pointer;
	public nint[] fences;
	public int currentFrame;
	public bool firstFrame = true;
	public bool dirty;
	public Span<Type> AsSpan<Type>(nint offset=0){
		this.dirty = true;
		var sections = this.fences.Length;
		var size = this.size / sections;
		var address = (byte*)this.pointer + size * this.currentFrame + offset;
		return new(address,(int)(size - offset) / Marshal.SizeOf<Type>());
	}
	public ReadOnlySpan<Type> AsReadOnlySpan<Type>(nint offset=0){
		var sections = this.fences.Length;
		var size = this.size / sections;
		var address = (byte*)this.pointer + size * this.currentFrame + offset;
		return new(address,(int)(size - offset) / Marshal.SizeOf<Type>());
	}
	public nint Count<Type>() => this.size / this.fences.Length / Marshal.SizeOf<Type>();
	public void WaitSync(){
		var fence = this.fences[this.currentFrame];
		if(fence == 0){return;}
		this.context.API.ClientWaitSync(fence,SyncObjectMask.Bit,ulong.MaxValue);
		this.context.API.DeleteSync(fence);
		this.fences[this.currentFrame] = 0;
		if(this.firstFrame){
			this.firstFrame = false;
			return;
		}
		if(!this.dirty){return;}
		var sections = this.fences.Length;
		var size = this.size / sections;
		var previousFrame = this.currentFrame == 0 ? sections - 1 : this.currentFrame - 1;
		var source = (byte*)this.pointer + size * previousFrame;
		var destination = (byte*)this.pointer + size * this.currentFrame;
		System.Buffer.MemoryCopy(source,destination,size,size);
		this.dirty = false;
	}
	public void NextFrame(){
		this.fences[this.currentFrame] = this.context.API.FenceSync(SyncCondition.SyncGpuCommandsComplete,SyncBehaviorFlags.None);
		this.currentFrame = (this.currentFrame + 1) % this.fences.Length;
	}
	public Buffer(nint size,string label="Buffer"){
		var flags = GLEnum.MapWriteBit|GLEnum.MapPersistentBit|GLEnum.MapCoherentBit;
		this.context = OpenGL.current;
		this.name = label;
		this.handle = this.context.API.CreateBuffer();
		this.context.API.ObjectLabel(GLEnum.Buffer,this.handle,(uint)this.name.Length,this.name);
		this.fences = new nint[Globals.syncRegions];
		this.size = size * this.fences.Length;
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