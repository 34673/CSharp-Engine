using Silk.NET.OpenGL;
namespace Engine.Renderer.OpenGL{
	public static class Globals{
		public static int maxVertexBindings;
		public static int maxUniformBindings;
		public static int maxShaderStorageBindings;
		public static int maxTextureUnits;
		public static void Start(GL API){
			Globals.maxVertexBindings = API.GetInteger(GLEnum.MaxVertexAttribBindings);
			Globals.maxUniformBindings = API.GetInteger(GLEnum.MaxUniformBufferBindings);
			Globals.maxShaderStorageBindings = API.GetInteger(GLEnum.MaxShaderStorageBufferBindings);
			Globals.maxTextureUnits = API.GetInteger(GLEnum.MaxTextureImageUnits);
		}
	}
}
