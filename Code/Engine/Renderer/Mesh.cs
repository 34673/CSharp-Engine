using System;
using System.Numerics;
namespace Engine.Renderer{
	public enum FaceType{Triangle,Quadratic,NGon};
	public class Mesh{
		public string name;
		public FaceType faceType;
		public Vector3[] vertices = Array.Empty<Vector3>();
		public Vector3[] normals = Array.Empty<Vector3>();
		public uint[] indices = Array.Empty<uint>();
        public Vector2[] uvs = Array.Empty<Vector2>();
        public Vector4[] colors = Array.Empty<Vector4>();
	}
}
