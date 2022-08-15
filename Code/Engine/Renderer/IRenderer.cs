namespace Engine.Renderer{
	public interface IRenderer{
        public void Start(){}
		public void Update(){}
        public void Update(double delta){}
        public void RegisterModel(string modelPath,string skinPath){}
        public void RegisterModel(Model model,Skin skin){}
	}
}
