namespace Engine.Renderer.OpenGL{
	public class RenderState{
		public VertexArray vertexArray;
		public Buffer[] vertexBuffers;
		public Buffer indexBuffer;
		public Buffer indirectBuffer;
		public Buffer[] uniformBuffers;
		public Buffer[] shaderStorageBuffers;
		public Texture[] textures;
		//renderbuffers here
		public Shader shader;
		public RenderState(){
			this.vertexBuffers = new Buffer[Globals.maxVertexBindings];
			this.uniformBuffers = new Buffer[Globals.maxUniformBindings];
			this.shaderStorageBuffers = new Buffer[Globals.maxShaderStorageBindings];
			this.textures = new Texture[Globals.maxTextureUnits];
		}
	}
}
