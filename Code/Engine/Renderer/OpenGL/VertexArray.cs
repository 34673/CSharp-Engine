using Silk.NET.OpenGL;
using System;
using System.Collections.Generic;
using System.Linq;
namespace Engine.Renderer.OpenGL{
	public class VertexArray : IDisposable{
		public GL renderer;
		public static List<Dictionary<string,int>> formats = new();
		public string name;
		public uint handle;
		public Dictionary<string,int> format = new();
		public (uint vertex,uint attribute) bindingIndex;
        public uint attributeIndex;
        public uint attributeOffset;
		public VertexArray(GL renderer){
            this.renderer = renderer;
            this.handle = this.renderer.CreateVertexArray();
			VertexArray.formats.Add(this.format);
        }
		public static VertexArray TryGet(int[] formatLengths){
			foreach(var format in VertexArray.formats){
				var reference = format.Values.ToArray();
				if(reference.Length != formatLengths.Length){return new(OpenGL.current.API);}
			}
			return new(OpenGL.current.API);
		}
		public void SetLabel(string name){
			this.name = name;
            this.renderer.ObjectLabel(GLEnum.Buffer,this.handle,(uint)name.Length,name);
		}
        public void AddVertexBuffer(uint bufferHandle,int vertexLength){
            var stride = (uint)(vertexLength * sizeof(float));
            this.renderer.VertexArrayVertexBuffer(this.handle,this.bindingIndex.vertex,bufferHandle,0,stride);
			this.bindingIndex.vertex += 1;
        }
        public void AddIndexBuffer(uint bufferHandle){
            this.renderer.VertexArrayElementBuffer(this.handle,bufferHandle);
        }
        public void AddAttribute(string name,int length){
			this.format[name] = length;
            this.renderer.EnableVertexArrayAttrib(this.handle,this.attributeIndex);
            this.renderer.VertexArrayAttribBinding(this.handle,this.attributeIndex,this.bindingIndex.attribute);
            this.renderer.VertexArrayAttribFormat(this.handle,this.attributeIndex,length,GLEnum.Float,false,this.attributeOffset);
			this.bindingIndex.attribute += 1;
            this.attributeIndex += 1;
            this.attributeOffset += (uint)length * sizeof(float);
        }
		public void Bind(){this.renderer.BindVertexArray(this.handle);}
		public void Dispose(){
			VertexArray.formats.Remove(this.format);
			this.renderer.DeleteVertexArray(this.handle);
		}
	}
}
