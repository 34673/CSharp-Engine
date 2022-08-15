using Silk.NET.Assimp;
using System;
using System.Collections.Generic;
using System.Numerics;
using File = System.IO.File;
namespace Engine.Renderer{
	public class Model{
        public static Dictionary<string,Model> all = new();
		public static Assimp importer = Assimp.GetApi();
		public Dictionary<string,Mesh> submeshes = new();
		public Dictionary<string,Material> materials = new();
        public string path;
		public string name;
        public Model(){}
		public Model(string path) => LoadFile(path);
		public static Model LoadFile(string path){
            var model = new Model();
			path = path.Replace('\\','/');
            if(!File.Exists(path)){return model;}
            model.path = path;
			model.name = path.Remove(path.LastIndexOf(".")).Substring(path.LastIndexOf('/')+1);
			unsafe{
				var scene = Model.importer.ImportFile(path,(uint)PostProcessSteps.None);
                if(scene is null){throw new(Model.importer.GetErrorStringS());}
				if(scene->MNumMeshes > 0){
					for(var meshIndex=0;meshIndex<scene->MNumMeshes;++meshIndex){
						var assimpMesh = scene->MMeshes[meshIndex];
						var mesh = model.submeshes[assimpMesh->MName] = new();
						var offset = 0;
						mesh.name = assimpMesh->MName;
						if(assimpMesh->MNumVertices > 0){
							mesh.vertices = new Span<Vector3>(assimpMesh->MVertices,(int)assimpMesh->MNumVertices).ToArray();
							mesh.normals = new Span<Vector3>(assimpMesh->MNormals,(int)assimpMesh->MNumVertices).ToArray();
						}
						if(assimpMesh->MNumFaces > 0){
							//Assume all faces have the same number of indices for now. Variable NGons support not planned for now.
							var indexPerFace = (int)assimpMesh->MFaces[0].MNumIndices;
							if(indexPerFace == 3){mesh.faceType = FaceType.Triangle;}
							if(indexPerFace == 4){mesh.faceType = FaceType.Quadratic;}
							if(indexPerFace > 4){mesh.faceType = FaceType.NGon;}
							mesh.indices = new uint[assimpMesh->MNumFaces*indexPerFace];
							for(var index=0;index<assimpMesh->MNumFaces;++index){
								mesh.indices = new Span<uint>(assimpMesh->MFaces[index].MIndices,indexPerFace).ToArray();
								offset += indexPerFace;
							}
						}
						model.materials[assimpMesh->MName] = Material.TryLoad("Default");
					}
				}
                Model.importer.FreeScene(scene);
			}
            return Model.all[model.path] = model;
		}
	}
}
