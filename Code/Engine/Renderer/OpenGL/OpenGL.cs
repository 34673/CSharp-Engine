using Silk.NET.Core.Native;
using Silk.NET.OpenGL;
using Silk.NET.OpenGL.Extensions.ARB;
using System;
using System.Collections.Generic;
namespace Engine.Renderer.OpenGL{
	using Engine;
	using Engine.Core;
	using Engine.Renderer;
	public class OpenGL : IRenderer{
		public static OpenGL current;
		public GL API;
		public ArbSparseBuffer SparseBuffer;
		public double delta;
		public OpenGL(){
			OpenGL.current = this;
			Program.window.Load += this.Start;
			Program.window.Render += this.Update;
		}
		public void Start(){
			this.API = GL.GetApi(Program.window);
			this.SparseBuffer = new(this.API.Context);
			Program.frameTimer.Elapsed += (a,b)=>Program.window.Title += " | Renderer (OpenGL): "+((int)(1/this.delta)).ToString()+" fps";
			this.API.Enable(EnableCap.DebugOutput|EnableCap.DepthTest);
			this.API.DebugMessageCallback(this.Log,(IntPtr)null);
			this.API.ClearColor(0f,0f,0.4f,0f);
			this.API.Enable(GLEnum.CullFace);
			this.API.CullFace(GLEnum.Back);
		}
		public void Update(double delta){
			this.delta = delta;
			this.API.Clear(ClearBufferMask.ColorBufferBit|ClearBufferMask.DepthBufferBit);
			foreach(var batch in RenderObject.all.Values){
				batch.shader.Use();
				batch.vertexArray.Bind();
				foreach(var (name,property) in batch.material.properties){
					batch.shader.SetUniform(name,property);
				}
				//this.API.DrawElements<uint>(PrimitiveType.Triangles,(uint)batch.indexBuffer.data.Length,DrawElementsType.UnsignedInt,null);
			}
			this.API.BindBuffer(GLEnum.DrawIndirectBuffer,RenderObject.commands.buffer.Handle);
			this.API.MultiDrawElementsIndirect<DrawElementsCommand>(PrimitiveType.Triangles,DrawElementsType.UnsignedInt,null,(uint)RenderObject.commands.data.Length,0);
		}
		public void Log(GLEnum source,GLEnum type,int id,GLEnum severity,int length,nint message,nint userParam){
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
		public void RegisterModel(string modelPath,string skinPath){
			this.RegisterModel(Model.LoadFile(modelPath),Skin.LoadFile(skinPath));
		}
		public void RegisterModel(Model model,Skin skin){
			foreach(var (name,mesh) in model.submeshes){
				var offset = 0;
				var components = 6;
				var vertexBuffer = new float[mesh.vertices.Length * components];
				for(var item=0;item<mesh.vertices.Length;++item){
					mesh.vertices.CopyTo(vertexBuffer,offset);
					mesh.normals.CopyTo(vertexBuffer,offset+3);
					offset += components;
				}
				var batch = new RenderObject();
				batch.vertexBuffer = new(this.API,vertexBuffer);
				batch.indexBuffer = new(this.API,mesh.indices);
				var vertexArray = batch.vertexArray = new(this.API);
				vertexArray.AddVertexBuffer(batch.vertexBuffer.buffer.Handle,0,6);
				vertexArray.AddIndexBuffer(batch.indexBuffer.buffer.Handle);
				vertexArray.AddAttribute("Positions",3,0);
				vertexArray.AddAttribute("Normals",3,0);
				skin.TryGetValue(mesh.name,out var materialName);
				batch.material = Material.TryLoad(materialName ?? "Default");
				batch.shader = Shader.TryLoad(batch.material.shader);
				this.API.BufferSubData<DrawElementsCommand>(GLEnum.DrawIndirectBuffer,RenderObject.commands.data.Length,RenderObject.commands.data);
				var command = new DrawElementsCommand();
				command.count = (uint)batch.vertexBuffer.data.Length;
				command.instances += 1;
				command.firstIndex = 0;
				command.baseVertex = 0;
				command.baseInstance = 0;
				RenderObject.all[model.name+"/"+name] = batch;
			}
		}
		/*public void RegisterModel(Model model,Skin skin){
			var pairings = new Dictionary<Material,List<Mesh>>();
			foreach(var submesh in model.submeshes){
				var meshName = submesh.Key;
				var materialName = skin.ContainsKey(meshName) ? skin[meshName] : "Default";
				var material = Material.TryLoad(materialName);
				if(!pairings.ContainsKey(material)){pairings[material] = new();}
				pairings[material].Add(submesh.Value);
			}
			foreach(var (material,meshList) in pairings){
				var batch = new Batch();
				var offset = 0;
				var components = 6;
				foreach(var mesh in meshList){
					var range = new float[mesh.vertices.Length * components];
					for(var item=0;item<mesh.vertices.Length;++item){
						range[offset] = mesh.vertices[item].X;
						range[offset+1] = mesh.vertices[item].Y;
						range[offset+2] = mesh.vertices[item].Z;
						range[offset+3] = mesh.normals[item].X;
						range[offset+4] = mesh.normals[item].Y;
						range[offset+5] = mesh.normals[item].Z;
						offset += components;
					}
					batch.vertexBuffer.AddRange(range);
					batch.indexBuffer.AddRange(mesh.indices);
				}
				var vertexBuffer = new BufferObject<float>(this.API,batch.vertexBuffer.ToArray());
				var indexBuffer = new BufferObject<uint>(this.API,batch.indexBuffer.ToArray());
				var vertexArray = new VertexArray(this.API);
				vertexArray.AddVertexBuffer(vertexBuffer.buffer.Handle,0,6);
				vertexArray.AddIndexBuffer(indexBuffer.buffer.Handle);
				vertexArray.AddAttribute("Positions",3,0);
				vertexArray.AddAttribute("Normals",3,0);
				batch.vertexArray = vertexArray;
				batch.material = material;
				batch.shader = Shader.TryLoad(material.shader);
				this.batches[model.path+"/"+material.name] = batch;
			}
		}*/
	}
}
