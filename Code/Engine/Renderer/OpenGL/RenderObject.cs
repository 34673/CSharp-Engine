using System.Collections.Generic;
using System.Linq;
namespace Engine.Renderer.OpenGL{
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
		public RenderObject(){
			this.uniformOffsets = new int[Globals.maxUniformBindings];
			this.uniformSizes = new nuint[Globals.maxUniformBindings];
			this.shaderStorageOffsets = new int[Globals.maxShaderStorageBindings];
			this.shaderStorageSizes = new nuint[Globals.maxShaderStorageBindings];
			this.shader = Shader.all["Default"];
			this.material = Material.all["Default"];
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
	}
	public struct DrawElementsCommand{
		public uint indexCount;
		public uint instances;
		public uint firstIndex;
		public uint baseVertex;
		public uint baseInstance;
	}
}
