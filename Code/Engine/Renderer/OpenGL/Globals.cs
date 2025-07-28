namespace Engine.Renderer.OpenGL;
using Silk.NET.OpenGL;
using System;
using System.Linq;
public static class Globals{
	public static int maxVertexBindings;
	public static int maxVertexAttributes;
	public static int maxUniformBindings;
	public static int maxShaderStorageBindings;
	public static int maxTextureUnits;
	public static void Start(GL API){
		Globals.maxVertexBindings = API.GetInteger(GLEnum.MaxVertexAttribBindings);
		Globals.maxVertexAttributes = API.GetInteger(GLEnum.MaxVertexAttribs);
		Globals.maxUniformBindings = API.GetInteger(GLEnum.MaxUniformBufferBindings);
		Globals.maxShaderStorageBindings = API.GetInteger(GLEnum.MaxShaderStorageBufferBindings);
		Globals.maxTextureUnits = API.GetInteger(GLEnum.MaxTextureImageUnits);
	}
	public static void PrintAll() {
		Console.WriteLine("OpenGL globals:");
		var fields = typeof(Globals).GetFields().ToList();
		fields.ForEach(x=>Console.WriteLine($"\t{x.Name}: {x.GetValue(x)}"));
	}
}