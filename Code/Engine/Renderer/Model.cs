namespace Engine.Renderer;
using Silk.NET.Assimp;
using System;
using System.Collections.Generic;
using System.Numerics;
using File = System.IO.File;
public class Model{
	public static Dictionary<string,Model> all = [];
	public static Assimp importer = Assimp.GetApi();
	public Dictionary<string,Mesh> submeshes = [];
	public Dictionary<string,Material> materials = [];
	public string path;
	public string name;
	public Model(){}
	public Model(string path) => LoadFile(path);
	public static unsafe Model LoadFile(string path){
		var model = new Model();
		path = path.Replace('\\','/');
		if(!File.Exists(path)){return model;}
		model.path = path;
		model.name = path[(path.LastIndexOf('/') + 1)..path.LastIndexOf('.')];
		var importFlags = PostProcessSteps.JoinIdenticalVertices|PostProcessSteps.GenerateNormals|PostProcessSteps.MakeLeftHanded|PostProcessSteps.FixInFacingNormals;
		var scene = Model.importer.ImportFile(path,(uint)importFlags);
		if(scene is null){throw new(Model.importer.GetErrorStringS());}
		if(scene->MNumMeshes > 0){
			for(var meshIndex=0;meshIndex<scene->MNumMeshes;++meshIndex){
				var assimpMesh = scene->MMeshes[meshIndex];
				var mesh = model.submeshes[assimpMesh->MName] = new(assimpMesh->MName);
				if(assimpMesh->MVertices is not null){
					mesh.vertices = new Vector3[assimpMesh->MNumVertices];
					new Span<Vector3>(assimpMesh->MVertices,(int)assimpMesh->MNumVertices).CopyTo(new(mesh.vertices));
					mesh.vertexFormat["Positions"] = new("Positions",typeof(float),3,1);
				}
				if(assimpMesh->MNormals is not null){
					mesh.normals = new Vector3[assimpMesh->MNumVertices];
					new Span<Vector3>(assimpMesh->MNormals,(int)assimpMesh->MNumVertices).CopyTo(new(mesh.normals));
					mesh.vertexFormat["Normals"] = new("Normals",typeof(float),3,1,false,true);
				}
				var uvChannels = 8;
				for(var channel = 0;channel < uvChannels;++channel){
					if(channel >= mesh.uvs.Length){break;}
					if(assimpMesh->MTextureCoords[channel] is null){continue;}
					var name = $"TextureCoordinates{channel}";
					mesh.uvs[channel] = new Vector3[assimpMesh->MNumVertices];
					new Span<Vector3>(assimpMesh->MTextureCoords[channel],(int)assimpMesh->MNumVertices).CopyTo(new(mesh.uvs[channel]));
					mesh.vertexFormat[name] = new(name,typeof(float),3,1);
				}
				var colorChannels = 8;
				for(var channel = 0;channel < colorChannels;++channel){
					if(channel >= mesh.colors.Length){break;}
					if(assimpMesh->MColors[channel] is null){continue;}
					var name = $"Colors{channel}";
					mesh.colors[channel] = new Vector4[assimpMesh->MNumVertices];
					new Span<Vector4>(assimpMesh->MColors[channel],(int)assimpMesh->MNumVertices).CopyTo(new(mesh.colors[channel]));
					mesh.vertexFormat[name] = new(name,typeof(float),4,1);
				}
				if(assimpMesh->MNumFaces > 0){
					var offset = 0;
					//Assume all faces have the same number of indices for now. Variable polygon support not planned at the moment.
					var indexPerFace = (int)assimpMesh->MFaces[0].MNumIndices;
					if(indexPerFace == 3){mesh.faceType = FaceType.Triangle;}
					if(indexPerFace == 4){mesh.faceType = FaceType.Quadratic;}
					if(indexPerFace > 4){mesh.faceType = FaceType.NGon;}
					mesh.indices = new uint[assimpMesh->MNumFaces * indexPerFace];
					for(var faceIndex = 0;faceIndex < assimpMesh->MNumFaces;faceIndex += 1){
						var face = &assimpMesh->MFaces[faceIndex];
						for(var index = 0;index < indexPerFace;index += 1){
							mesh.indices[offset+index] = face->MIndices[index];
						}
						offset += indexPerFace;
					}
				}
				model.materials[assimpMesh->MName] = Material.TryLoad("Default");
			}
		}
		Model.importer.FreeScene(scene);
		return Model.all[model.path] = model;
	}
}