namespace Engine.Renderer;
using System.Collections.Generic;
using System.Numerics;
public class Transform(Transform parent=null){
	public static Dictionary<string,Transform> all = [];
	public Transform parent = parent;
	public Vector3 up => Vector3.Transform(Vector3.UnitY,this.rotation);
	public Vector3 right => Vector3.Transform(Vector3.UnitX,this.rotation);
	public Vector3 front => Vector3.Transform(Vector3.UnitZ,this.rotation);
	public Vector3 down => -up;
	public Vector3 left => -right;
	public Vector3 back => -front;
	public Vector3 position;
	public Quaternion rotation;
	public float scale;
	public Matrix4x4 matrix => Matrix4x4.Identity * Matrix4x4.CreateFromQuaternion(this.rotation) * Matrix4x4.CreateScale(this.scale) * Matrix4x4.CreateTranslation(this.position);
}