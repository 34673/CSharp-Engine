using Silk.NET.OpenGL;
using Silk.NET.OpenGL.Extensions.ARB;
using System;
using GLBuffer = Silk.NET.OpenGL.Buffer;
namespace Engine.Renderer.OpenGL{
	public class Buffer<Type> : IDisposable where Type : unmanaged{
		public GL renderer;
		public GLBuffer buffer;
		public string name;
		public bool persistent;
		public Buffer(GL renderer){
			this.renderer = renderer;
            this.renderer.CreateBuffers(1,out this.buffer);
		}
		public void SetLabel(string name){
			this.name = name;
            this.renderer.ObjectLabel(GLEnum.Buffer,this.buffer.Handle,(uint)name.Length,name);
		}
		public void SetData(Span<Type> data,bool persistent=false,int mapOffset=0){
			var flags = GLEnum.DynamicStorageBit;
			var mapBits = GLEnum.MapWriteBit|GLEnum.MapReadBit|GLEnum.MapPersistentBit|GLEnum.MapCoherentBit;
            flags |= persistent ? mapBits : (GLEnum)ARB.SparseStorageBitArb;
            this.renderer.NamedBufferStorage<Type>(this.buffer.Handle,data,(uint)flags);
			if(!persistent){return;}
			this.persistent = true;
			unsafe{
				this.renderer.MapNamedBufferRange(this.buffer.Handle,mapOffset,(nuint)data.Length,(uint)flags);
			}
		}
		public void SetRange(Span<Type> data,int offset=0){
			this.renderer.NamedBufferSubData<Type>(this.buffer.Handle,offset,(uint)data.Length,data);
		}
		public void Commit(int startIndex,int length){
			if(this.persistent){return;}
			OpenGL.current.SparseBuffer.NamedBufferPageCommitment(this.buffer.Handle,startIndex,(uint)length,true);
		}
		public void Decommit(int startIndex,int length){
			if(this.persistent){return;}
			OpenGL.current.SparseBuffer.NamedBufferPageCommitment(this.buffer.Handle,startIndex,(uint)length,false);
		}
		public void Dispose(){
			if(this.persistent){this.renderer.UnmapNamedBuffer(this.buffer.Handle);}
			this.renderer.DeleteBuffers(1,this.buffer.Handle);
		}
	}
}
