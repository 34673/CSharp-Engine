using System;
using System.Collections.Generic;
using System.Numerics;
namespace Engine.Renderer{
	public enum FaceType{Triangle,Quadratic,NGon};
	public class VertexAttribute(string name="Attribute",Type type=null,int count=0,int layers=1,bool packed=false,bool normalized=false){
		public string name = name;
		public Type type = type ?? typeof(float);	//Type of the smallest unit of data in the attribute (int, float, bool)
		public int count = count;					//Amount of <Type> sized items making a single attribute element
		public int layers = layers;					//Amount of additional sets of data (ex: vertex animations)
		public bool packed = packed;				//Data should be packed rather than interleaved
		public bool normalized = normalized;
	}
	public class Mesh(string name="Mesh"){
		public string name = name;
		public FaceType faceType;
		public Dictionary<string,VertexAttribute> vertexFormat = [];
		public Vector3[] vertices = [];
		public Vector3[] normals = [];
        public Vector3[][] uvs = new Vector3[8][];
        public Vector4[][] colors = new Vector4[8][];
		public uint[] indices = [];
	}
}
