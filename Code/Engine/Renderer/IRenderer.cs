namespace Engine.Renderer{
	public interface IRenderer{
        public void Start(){}
		public void Update(){}
        public void Update(double delta){}
        public void Stop(){}
        public void AddModel(string modelPath,string skinPath){}
        public void AddModel(Model model,Skin skin){}
		public void RemoveModel(string name){}
	}
}
