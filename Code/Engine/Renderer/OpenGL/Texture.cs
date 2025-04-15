namespace Engine.Renderer.OpenGL;
using Silk.NET.OpenGL;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
public class Texture : IDisposable{
	public static Dictionary<string,Texture> all = [];
	public string path;
	public uint handle;
	public GL renderer;
	public Texture(GL renderer,string path){
		if(!File.Exists(path)){return;}
		using var image = Image.Load<Rgba32>(path);
		this.renderer = renderer;
		this.handle = this.renderer.GenTexture();
		this.Bind();
		var (width,height) = ((uint)image.Width,(uint)image.Height);
		renderer.TexImage2D<byte>(TextureTarget.Texture2D,0,InternalFormat.Rgba8,width,height,0,PixelFormat.Rgba,PixelType.UnsignedByte,null);
		image.ProcessPixelRows(accessor =>{
			for(var row=0;row<height;++row){
				renderer.TexSubImage2D<Rgba32>(TextureTarget.Texture2D,0,0,row,(uint)accessor.Width,1,PixelFormat.Rgba,PixelType.UnsignedByte,accessor.GetRowSpan(row));
			}
		});
		this.SetParameters();
	}
	public Texture(GL renderer,Span<byte> data,Vector2 resolution){
		this.renderer = renderer;
		this.handle = this.renderer.GenTexture();
		this.Bind();
		this.renderer.TexImage2D<byte>(TextureTarget.Texture2D,0,InternalFormat.Rgba,(uint)resolution.X,(uint)resolution.Y,0,PixelFormat.Rgba,PixelType.UnsignedByte,data);
		this.SetParameters();
	}
	public void SetParameters(){
		this.renderer.TexParameter(TextureTarget.Texture2D,TextureParameterName.TextureWrapS,(int)GLEnum.ClampToEdge);
		this.renderer.TexParameter(TextureTarget.Texture2D,TextureParameterName.TextureWrapT,(int)GLEnum.ClampToEdge);
		this.renderer.TexParameter(TextureTarget.Texture2D,TextureParameterName.TextureMinFilter,(int)GLEnum.Linear);
		this.renderer.TexParameter(TextureTarget.Texture2D,TextureParameterName.TextureMagFilter,(int)GLEnum.Linear);
		this.renderer.GenerateMipmap(TextureTarget.Texture2D);
	}
	public void Bind(TextureUnit textureSlot=TextureUnit.Texture0){
		this.renderer.ActiveTexture(textureSlot);
		this.renderer.BindTexture(TextureTarget.Texture2D,this.handle);
	}
	public void Dispose() => this.renderer.DeleteTexture(this.handle);
}