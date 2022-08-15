using System.Collections.Generic;
namespace Engine.Renderer.OpenGL{
	public class RenderObject{
		public static SortedDictionary<string,RenderObject> all = new();
		public static Buffer<DrawElementsCommand> commands = new(OpenGL.current.API,new DrawElementsCommand[RenderObject.maxAmount],true);
		public static Buffer<float> vertexBuffer;
		public static Buffer<uint> indexBuffer;
        public static VertexArray vertexArray;
		public static int maxAmount = 100000;
		public static int lastCommand;
		public List<float> vertexList = new();
		public List<uint> indexList = new();
		public DrawElementsCommand command;
		public (int vertex,int index,int command) bufferOffset;
        public Material material;
        public Shader shader;
		public static void UpdateBuffers(){
			var vertices = new List<float>();
			var indices = new List<uint>();
			foreach(var batch in RenderObject.all){
				
			}
		}
	}
	public struct DrawElementsCommand{
		public uint totalVertices;
		public uint instances;
		public uint firstIndex;
		public int firstVertex;
		public uint baseInstance;
	}
}
