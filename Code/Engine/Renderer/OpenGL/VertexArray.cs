using Silk.NET.OpenGL;
using System;
using GLArray = Silk.NET.OpenGL.VertexArray;
namespace Engine.Renderer.OpenGL{
	public class VertexArray : IDisposable{
		public GL renderer;
		public GLArray array;
		public string name;
        public uint attributeIndex;
        public uint attributeOffset;
		public VertexArray(GL renderer){
            this.renderer = renderer;
            this.renderer.CreateVertexArrays(1,out this.array);
        }
		public void SetLabel(string name){
			this.name = name;
            this.renderer.ObjectLabel(GLEnum.Buffer,this.array.Handle,(uint)name.Length,name);
		}
        public void AddVertexBuffer(uint bufferHandle,int bindingIndex,int vertexLength){
            var stride = (uint)(vertexLength * sizeof(float));
            this.renderer.VertexArrayVertexBuffer(this.array.Handle,(uint)bindingIndex,bufferHandle,0,stride);
        }
        public void AddIndexBuffer(uint bufferHandle){
            this.renderer.VertexArrayElementBuffer(this.array.Handle,bufferHandle);
        }
        public void AddAttribute(string name,int length,int bindingIndex){
            this.renderer.EnableVertexArrayAttrib(this.array.Handle,this.attributeIndex);
            this.renderer.VertexArrayAttribBinding(this.array.Handle,this.attributeIndex,(uint)bindingIndex);
            this.renderer.VertexArrayAttribFormat(this.array.Handle,this.attributeIndex,length,GLEnum.Float,false,this.attributeOffset);
            this.attributeIndex += 1;
            this.attributeOffset += (uint)length * sizeof(float);
        }
		public void Bind() => this.renderer.BindVertexArray(this.array.Handle);
		public void Dispose() => this.renderer.DeleteVertexArray(this.array.Handle);
	}
}
