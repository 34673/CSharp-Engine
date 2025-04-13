using System;
using System.Numerics;
namespace Game{
    public class Camera : Entity{
        public static Camera main;
        public float fieldOfView = 90f;
        public (float minimum,float maximum) zoomBounds = (1f,45f);
        public float zoom => Math.Clamp(1f,this.zoomBounds.minimum,this.zoomBounds.maximum);
        public float aspectRatio = 1920/1080;
        public Camera() : base(){}
        public Matrix4x4 viewMatrix => Matrix4x4.CreateLookAt(this.transform.position,this.transform.position + this.transform.front,this.transform.up);
        public Matrix4x4 projectionMatrix => Matrix4x4.CreatePerspectiveFieldOfView(float.DegreesToRadians(this.zoom),this.aspectRatio,0.1f,100f);
    }
}
