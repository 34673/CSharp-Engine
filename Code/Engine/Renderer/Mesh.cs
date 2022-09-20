using System;
using System.Collections.Generic;
using System.Numerics;
namespace Engine.Renderer{
	public enum FaceType{Triangle,Quadratic,NGon};
	public class Mesh{
		public string name;
		public FaceType faceType;
		public Dictionary<string,List<object>> vertexAttributes = new();
		public Vector3[] vertices = Array.Empty<Vector3>();
		public Vector3[] normals = Array.Empty<Vector3>();
        public Vector2[] uvs = Array.Empty<Vector2>();
        public Vector4[] colors = Array.Empty<Vector4>();
		public uint[] indices = Array.Empty<uint>();
	}
}
