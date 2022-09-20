using Silk.NET.OpenGL;
using Silk.NET.OpenGL.Extensions.ARB;
using System;
using System.Collections.Generic;
namespace Engine.Renderer.OpenGL{
	public class Buffer<Type> : IDisposable where Type : unmanaged{
		public GL renderer;
		public string name;
		public uint handle;
		public bool persistent;
		public List<Type> data = new();
		public Buffer(GL renderer){
			this.renderer = renderer;
            this.handle = this.renderer.CreateBuffer();
		}
		public void SetLabel(string name){
			this.name = name;
            this.renderer.ObjectLabel(GLEnum.Buffer,this.handle,(uint)name.Length,name);
		}
		public void SetData(Span<Type> data,bool persistent=false,int mapOffset=0){
			var flags = GLEnum.DynamicStorageBit;
			var mapBits = GLEnum.MapWriteBit|GLEnum.MapReadBit|GLEnum.MapPersistentBit|GLEnum.MapCoherentBit;
            flags |= persistent ? mapBits : (GLEnum)ARB.SparseStorageBitArb;
			this.data = new(data.ToArray());
            this.renderer.NamedBufferStorage<Type>(this.handle,data,(uint)flags);
			if(!persistent){return;}
			this.persistent = true;
			unsafe{
				this.renderer.MapNamedBufferRange(this.handle,mapOffset,(nuint)data.Length,(uint)flags);
			}
		}
		public void SetRange(Span<Type> data,int startIndex=0){
			this.renderer.NamedBufferSubData<Type>(this.handle,startIndex,(uint)data.Length,data);
		}
		public void Commit(int startIndex,int length){
			if(this.persistent){return;}
			OpenGL.current.SparseBuffer.NamedBufferPageCommitment(this.handle,startIndex,(uint)length,true);
		}
		public void Decommit(int startIndex,int length){
			if(this.persistent){return;}
			OpenGL.current.SparseBuffer.NamedBufferPageCommitment(this.handle,startIndex,(uint)length,false);
		}
		public void Dispose(){
			if(this.persistent){this.renderer.UnmapNamedBuffer(this.handle);}
			this.renderer.DeleteBuffers(1,this.handle);
		}
	}
}
