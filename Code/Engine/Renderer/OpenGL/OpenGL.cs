namespace Engine.Renderer.OpenGL;
using Engine.Renderer;
using Silk.NET.Core.Native;
using Silk.NET.OpenGL;
using Silk.NET.OpenGL.Extensions.ARB;
using System;
using System.Linq;
using System.Runtime.InteropServices;
using Program = Engine.Core.Program;
public class OpenGL : IRenderer{
	public static OpenGL current;
	public static string defaultMaterialPath = "Shaders/@Default.material";
	public GL API;
	public RenderState renderState;
	public double delta;
	public OpenGL(){
		OpenGL.current = this;
		Import.window.Load += this.Start;
		Import.window.Render += this.Update;
	}
	public static void Log(GLEnum source,GLEnum type,GLEnum severity,string message){
		OpenGL.Log(source,type,0,severity,message.Length,SilkMarshal.StringToPtr(message));
	}
	public static void Log(GLEnum source,GLEnum type,int id,GLEnum severity,int length,nint message,nint userParam=0){
		var color = Console.ForegroundColor;
		//if(severity is GLEnum.DebugSeverityNotification){return;}
		if(severity is GLEnum.DebugSeverityHigh){color = ConsoleColor.Red;}
		if(severity is GLEnum.DebugSeverityMedium or GLEnum.DebugSeverityLow){color = ConsoleColor.Yellow;}
		Console.ForegroundColor = color;
		Console.Write("[OpenGL] ");
		Console.Write($"{source}".Replace("DebugSource","From: ") + "|");
		Console.Write($"{type}".Replace("DebugType","Type: ") + ": ");	
		Console.WriteLine(SilkMarshal.PtrToString(message));
		Console.ResetColor();
	}
	public void Start(){
		Program.frameTimer.Elapsed += (a,b)=>Import.window.Title += $" | Renderer (OpenGL): {(int)(1 / this.delta)} fps";
		var debugIDs = (uint)0;
		var debugParameters = (nint)0;
		this.API = GL.GetApi(Import.window);
		Extensions.Start();
		Material.fallback = Material.TryLoad(OpenGL.defaultMaterialPath);
		Shader.fallback = Shader.TryLoad(Material.fallback);
		this.API.Enable(EnableCap.DebugOutput|EnableCap.DepthTest|EnableCap.CullFace);
		this.API.DebugMessageControl(DebugSource.DontCare,DebugType.DontCare,DebugSeverity.DontCare,0,ref debugIDs,true);
		this.API.DebugMessageCallback(OpenGL.Log,ref debugParameters);
		Globals.Start(this.API);
		this.API.ClearColor(0f,0f,0.4f,0f);
		this.API.CullFace(GLEnum.Back);
		this.renderState = new();
		Globals.PrintAll();
	}
	public void Update(double delta){
		this.delta = delta;
		RenderObject.Sort();
		this.API.Clear(ClearBufferMask.ColorBufferBit|ClearBufferMask.DepthBufferBit|ClearBufferMask.StencilBufferBit);
		foreach(var renderObject in RenderObject.all.Values){
			renderObject.ApplyStates();
			//renderObject.shader.SetUniform("objectMatrix",renderObject.transform.matrix);
			this.API.MultiDrawElementsIndirect<DrawElementsCommand>(PrimitiveType.Triangles,DrawElementsType.UnsignedInt,null,1,0);
		}
	}
	public unsafe void AddObject(Mesh mesh,Material material,Transform transform){
		var objectKey = $"{mesh.path}+{material.path}";
		RenderObject.all.TryGetValue(objectKey,out var renderObject);
		if(renderObject is not null){
			renderObject = RenderObject.all[objectKey];
			renderObject.indirectBuffer.AsSpan<DrawElementsCommand>()[renderObject.indirectOffset].instances += 1;
			//Add other instance-related data (textures, uniforms, etc...)
			return;
		}
		renderObject = RenderObject.all[objectKey] = new();
		var format = mesh.vertexFormat.Values;
		renderObject.vertexArray = VertexArray.TryGet(format);
		if(renderObject.vertexArray is null){
			renderObject.vertexArray = new();
			renderObject.vertexArray.AddAttributes(format);
		}
		var vertexFormat = mesh.vertexFormat.Values.ToArray();
		var dataSize = vertexFormat.Sum(x=>Marshal.SizeOf(x.type) * x.count * x.layers) * mesh.vertices.Length;
		renderObject.vertexBuffers[0] = new(dataSize,$"{mesh.name} Vertex Buffer");
		var destination = (byte*)renderObject.vertexBuffers[0].pointer;
		for(var vertex=0;vertex<mesh.vertices.Length;++vertex){
			mesh.vertices[vertex].CopyTo(new Span<float>(destination,Marshal.SizeOf(mesh.vertices[vertex]) / sizeof(float)));
			destination += Marshal.SizeOf(mesh.vertices[vertex]);
			if(mesh.normals.Length > 0){
				mesh.normals[vertex].CopyTo(new Span<float>(destination,Marshal.SizeOf(mesh.normals[vertex]) / sizeof(float)));
				destination += Marshal.SizeOf(mesh.normals[vertex]);
			}
			foreach(var channel in mesh.uvs){
				if(channel is null){continue;}
				channel[vertex].CopyTo(new Span<float>(destination,Marshal.SizeOf(channel[vertex]) / sizeof(float)));
				destination += Marshal.SizeOf(channel[vertex]);
			}
			foreach(var channel in mesh.colors){
				if(channel is null){continue;}
				channel[vertex].CopyTo(new Span<float>(destination,Marshal.SizeOf(channel[vertex]) / sizeof(float)));
				destination += Marshal.SizeOf(channel[vertex]);
			}
		}
		renderObject.indexBuffer = new(sizeof(uint) * mesh.indices.Length,$"{mesh.name} Index Buffer");
		mesh.indices.AsSpan().CopyTo(renderObject.indexBuffer.AsSpan<uint>());
		renderObject.indirectBuffer = new(sizeof(DrawElementsCommand),$"{mesh.name} Indirect Buffer");
		var indirectBuffer = renderObject.indirectBuffer.AsSpan<DrawElementsCommand>();
		indirectBuffer[0].baseInstance = 0;
		indirectBuffer[0].baseVertex = 0;
		indirectBuffer[0].firstIndex = 0;
		indirectBuffer[0].indexCount = (uint)renderObject.indexBuffer.Count<uint>();
		indirectBuffer[0].instances = 1;
		renderObject.material = Material.TryLoad(material.path);
		renderObject.shader = Shader.TryLoad(renderObject.material);
		foreach(var block in renderObject.shader.uniformBlocks.Values){
			renderObject.uniformBuffers[block.binding] = new((nint)block.size);
			renderObject.uniformSizes[block.binding] = block.size;
		}
		foreach(var block in renderObject.shader.shaderStorageBlocks.Values){
			renderObject.shaderStorageBuffers[block.binding] = new((nint)block.size);
			renderObject.shaderStorageSizes[block.binding] = block.size;
		}
		renderObject.ApplyMaterial();
	}
}