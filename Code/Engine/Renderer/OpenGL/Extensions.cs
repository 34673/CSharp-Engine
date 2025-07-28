using Silk.NET.Core.Native;
using Silk.NET.OpenGL;
using Silk.NET.OpenGL.Extensions.ARB;
using System;
using System.Collections.Generic;
namespace Engine.Renderer.OpenGL; 
public static class Extensions{
	public static Dictionary<string,NativeExtension<GL>> all = [];
	public static void Start(){
		Extensions.all["ARB_sparse_buffers"] = new ArbSparseBuffer(OpenGL.current.API.Context);
		var parallelCompile = new ArbParallelShaderCompile(OpenGL.current.API.Context);
		parallelCompile.MaxShaderCompilerThreads((uint)Environment.ProcessorCount - 1);
		Extensions.all["ARB_parallel_shader_compile"] = parallelCompile;
	}
}
