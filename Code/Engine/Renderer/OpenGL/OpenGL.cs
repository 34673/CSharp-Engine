using Silk.NET.Core.Native;
using Silk.NET.OpenGL;
using Silk.NET.OpenGL.Extensions.ARB;
using System;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
namespace Engine.Renderer.OpenGL{
	using Engine.Core;
	using Engine.Renderer;
	using SixLabors.ImageSharp.Memory;
	using System.Collections.Generic;

	public class OpenGL : IRenderer{
		public static OpenGL current;
		public GL API;
		public ArbSparseBuffer SparseBuffer;
        public List<VertexArray> vertexArrays = new();
		public Dictionary<VertexArray,Buffer<float>> vertexBuffers = new();
		public Dictionary<VertexArray,Buffer<uint>> indexBuffers = new();
		public Dictionary<VertexArray,Buffer<DrawElementsCommand>> commandBuffers = new();
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
			//foreach(var batch in RenderObject.All){
				/*foreach(var (name,property) in batch.material.properties){
					batch.shader.SetUniform(name,property);
				}
				this.API.DrawElements<uint>(PrimitiveType.Triangles,(uint)batch.indexBuffer.data.Length,DrawElementsType.UnsignedInt,null);
			}*/
			foreach(var array in this.vertexArrays){
				var commands = this.commandBuffers[array];
				array.Bind();
				Shader.all["Default"].Use();
				this.API.BindBuffer(GLEnum.DrawIndirectBuffer,commands.handle);
				this.API.MultiDrawElementsIndirect<DrawElementsCommand>(PrimitiveType.Triangles,DrawElementsType.UnsignedInt,null,(uint)commands.data.Count,0);
			}
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
		public void AddModel(string modelPath,string skinPath){
			this.AddModel(Model.LoadFile(modelPath),Skin.LoadFile(skinPath));
		}
		public void AddModel(Model model,Skin skin){
			if(!RenderObject.all.ContainsKey(model.name)){RenderObject.all[model.name] = new();}
			var renderObject = RenderObject.all[model.name];
			foreach(var (meshName,mesh) in model.submeshes){
				var format = mesh.vertexAttributes.Values.Select(x=>Marshal.SizeOf(x[0]) / sizeof(float)).ToArray();
				var dataSize = mesh.vertexAttributes.Values.Sum(x=>Marshal.SizeOf(x[0]) / sizeof(float) * x.Count);
				var vertexData = new float[dataSize];
				var offset = 0;
				renderObject.vertexArray = VertexArray.TryGet(format);
				for(var item=0;item<mesh.vertices.Length;++item){
					foreach(var (name,attributes) in mesh.vertexAttributes){
						var size = Marshal.SizeOf(attributes[0]) / sizeof(float);
						if(size > 3){((Vector4)attributes[item]).CopyTo(vertexData,offset);}
						else if(size > 2){((Vector3)attributes[item]).CopyTo(vertexData,offset);}
						else if(size > 1){((Vector2)attributes[item]).CopyTo(vertexData,offset);}
						if(renderObject.vertexArray.format.Count != format.Length){
							renderObject.vertexArray.AddAttribute(name,size);
						}
						offset += size;
					}
				}
				renderObject.vertexList.AddRange(vertexData);
				renderObject.indexList.AddRange(mesh.indices);
				skin.TryGetValue(mesh.name,out var materialName);
				renderObject.material = Material.TryLoad(materialName ?? "Default");
				renderObject.shader = Shader.TryLoad(renderObject.material.shader);
				renderObject.CheckBuffers();
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
