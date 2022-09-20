using Silk.NET.OpenGL;
using System;
using System.Collections.Generic;
using System.Linq;
namespace Engine.Renderer.OpenGL{
	public class RenderObject{
		public static Dictionary<string,RenderObject> all = new();
		public Buffer<float> vertexBuffer;
		public Buffer<uint> indexBuffer;
		public Buffer<DrawElementsCommand> commandBuffer;
		public DrawElementsCommand command;
		public List<float> vertexList = new();
		public List<uint> indexList = new();
		public (int vertex,int index,int command) bufferOffset;
		public VertexArray vertexArray;
        public Material material;
        public Shader shader;
		public RenderObject(){
			var renderer = OpenGL.current;
			renderer.vertexBuffers[this.vertexArray] = this.vertexBuffer = new(renderer.API);
			renderer.indexBuffers[this.vertexArray] = this.indexBuffer = new(renderer.API);
			renderer.commandBuffers[this.vertexArray] = this.commandBuffer = new(renderer.API);
		}
		public void UpdateBuffers(bool decommit=false){
			var renderer = OpenGL.current;
				this.command.instances += 1;
				this.commandBuffer.data[this.bufferOffset.command] = this.command;
			this.vertexArray.AddVertexBuffer(this.vertexBuffer.handle,this.vertexArray.format.Values.Sum());
			this.vertexArray.AddIndexBuffer(this.indexBuffer.handle);
			var offset = this.bufferOffset.command;
			var length = this.commandBuffer.data.Count-offset;
			var span = new Span<DrawElementsCommand>(commandBuffer.data.ToArray(),offset,length);
			renderer.API.NamedBufferSubData<DrawElementsCommand>(commandBuffer.handle,offset,(uint)length,span);
			if(this.vertexBuffer != null && this.command.instances < 1){
				this.command.totalVertices = (uint)this.vertexList.Count;
				this.command.instances = 1;
				this.command.firstIndex = (uint)this.bufferOffset.index * sizeof(uint);
				this.command.firstVertex = (uint)this.bufferOffset.vertex;
				this.command.baseInstance = 0;
				this.commandBuffer.data[this.bufferOffset.command] = this.command;
			}
			var command = new DrawElementsCommand();
		}
	}
	public struct DrawElementsCommand{
		public uint totalVertices;
		public uint instances;
		public uint firstIndex;
		public uint firstVertex;
		public uint baseInstance;
	}
}
