using System.Numerics;
using Engine.Core;
using Engine.Renderer;
namespace Game{
    public class Entity{
        public double delta;
        public Transform transform = new();
        public Entity(){
            Program.window.Update += this.Update;
            this.transform.rotation = Quaternion.Identity;
            this.transform.scale = 1f;
            this.Start();
        }
        ~Entity() => Program.window.Update -= this.Update;
        public virtual void Start(){}
        public virtual void Update(double delta){
            this.delta = delta;
        }
    }
}
 
