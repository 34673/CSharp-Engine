namespace Engine.Renderer.OpenGL;
using Engine.Renderer;
using Silk.NET.Core.Native;
using Silk.NET.OpenGL;
using Silk.NET.OpenGL.Extensions.ARB;
using System;
using System.Linq;
using System.Runtime.InteropServices;
using CoreProgram = Engine.Core.Program;
public class OpenGL : IRenderer{
	public static OpenGL current;
	public GL API;
	public ArbSparseBuffer SparseBuffer;
	public RenderState renderState;
	public double delta;
	public OpenGL(){
		OpenGL.current = this;
		CoreProgram.window.Load += this.Start;
		CoreProgram.window.Render += this.Update;
	}
	public static void Log(GLEnum source,GLEnum type,int id,GLEnum severity,int length,nint message,nint userParam){
		var color = Console.ForegroundColor;
		//if(severity is GLEnum.DebugSeverityNotification){return;}
		if(severity is GLEnum.DebugSeverityHigh){color = ConsoleColor.Red;}
		if(severity is GLEnum.DebugSeverityMedium or GLEnum.DebugSeverityLow){color = ConsoleColor.Yellow;}
		Console.ForegroundColor = color;
		Console.Write("[OpenGL] ");
		Console.Write(source.ToString().Replace("DebugSource","")+".");
		Console.Write(type.ToString().Replace("DebugType","")+": ");
		Console.WriteLine(SilkMarshal.PtrToString(message));
		Console.ResetColor();
	}
	public void Start(){
		this.API = GL.GetApi(CoreProgram.window);
		this.SparseBuffer = new(this.API.Context);
		CoreProgram.frameTimer.Elapsed += (a,b)=>CoreProgram.window.Title += $" | Renderer (OpenGL): {(int)(1 / this.delta)} fps";
		this.API.Enable(EnableCap.DebugOutputSynchronous|EnableCap.DepthTest|EnableCap.CullFace);
		this.API.DebugMessageControl(DebugSource.DontCare,DebugType.DontCare,DebugSeverity.DontCare,0,0,true);
		this.API.DebugMessageCallback(OpenGL.Log,(nint)null);
		Globals.Start(this.API);
		this.API.ClearColor(0f,0f,0.4f,0f);
		this.API.CullFace(GLEnum.Back);
		this.renderState = new();
	}
	public void Update(double delta){
		this.delta = delta;
		RenderObject.Sort();
		this.API.Clear(ClearBufferMask.ColorBufferBit|ClearBufferMask.DepthBufferBit);
		foreach(var renderObject in RenderObject.all.Values){
			this.CheckRenderState(renderObject);
			foreach(var (name,property) in renderObject.material.properties){
				renderObject.shader.SetUniform(name,property);
			}
			this.API.MultiDrawElementsIndirect<DrawElementsCommand>(PrimitiveType.Triangles,DrawElementsType.UnsignedInt,null,1,0);
		}
	}
	public void CheckRenderState(RenderObject renderObject){
		var globalState = this.renderState;
		renderObject.indirectBuffer.BindIndirect();
		renderObject.vertexArray.Bind();
		renderObject.vertexArray.SetIndexBuffer(renderObject.indexBuffer);
		for(var index = 0; index < renderObject.vertexBuffers.Length; index += 1){
			if(renderObject.vertexBuffers[index] is null){continue;}
			renderObject.vertexArray.SetVertexBuffer(renderObject.vertexBuffers[index],index,renderObject.vertexOffset);
		}
		for(var index = 0; index < renderObject.uniformBuffers.Length; index += 1){
			if(renderObject.uniformBuffers[index] is null){continue;}
			renderObject.uniformBuffers[index].BindRange(true,renderObject.uniformOffsets[index],renderObject.uniformSizes[index]);
		}
		for(var index = 0; index < renderObject.shaderStorageBuffers.Length; index += 1){
			if(renderObject.shaderStorageBuffers[index] is null){continue;}
			renderObject.shaderStorageBuffers[index].BindRange(false,renderObject.shaderStorageOffsets[index],renderObject.shaderStorageSizes[index]);
		}
		for(var index = 0; index < renderObject.textures.Length; index += 1){
			if(globalState.textures[index] == renderObject.textures[index]){continue;}
			renderObject.textures[index].Bind((TextureUnit)index);
		}
		if(globalState.shader != renderObject.shader){renderObject.shader.Use();}
	}
	public void AddModel(string modelPath,string skinPath) => this.AddModel(Model.LoadFile(modelPath),Skin.LoadFile(skinPath));
	public unsafe void AddModel(Model model,Skin skin){
		RenderObject.all.TryGetValue(model.name,out var renderObject);
		if(renderObject is not null){
			renderObject = RenderObject.all[model.name];
			renderObject.indirectBuffer.AsSpan<DrawElementsCommand>()[renderObject.indirectOffset].instances += 1;
			//Add other instance-related data (textures, uniforms, etc...)
			return;
		}
		renderObject = RenderObject.all[model.name] = new();
		var firstMesh = model.submeshes.First().Value;
		var format = firstMesh.vertexFormat.Values;
		renderObject.vertexArray = VertexArray.TryGet(format);
		if(renderObject.vertexArray is null){
			renderObject.vertexArray = new();
			renderObject.vertexArray.AddAttributes(firstMesh.vertexFormat.Values);
		}
		foreach(var mesh in model.submeshes.Values){
			var data = mesh.vertexFormat.Values.ToArray();
			var dataSize = data.Sum(x=>Marshal.SizeOf(x.type) * x.count * x.layers) * mesh.vertices.Length;
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
			skin.TryGetValue(mesh.name,out var materialName);
			renderObject.material = Material.TryLoad(materialName ?? "Default");
			renderObject.shader = Shader.TryLoad(renderObject.material.shader);
		}
	}
}