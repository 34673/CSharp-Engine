namespace Engine.Core;
using System.Collections.Generic;
using System.Reflection;
public static class Loader{
	public static string fileExtension = ".dll";
	public static Dictionary<string,Assembly> assemblies = [];
	public static Dictionary<string,List<object>> instances = [];
	public static Assembly GetAssembly(string relativePath){
		var key = relativePath.Replace(Loader.fileExtension,"");
		if(Loader.assemblies.ContainsKey(key)){return Loader.assemblies[key];}
		var assembly = Assembly.LoadFrom(FileSystem.baseDirectory+relativePath+Loader.fileExtension);
		if(assembly != null){Loader.assemblies[key] = assembly;}
		return assembly;
	}
	public static object Instantiate(Assembly assembly,string classPath){
		var key = assembly.Location.Replace(Loader.fileExtension,"");
		if(!Loader.assemblies.ContainsKey(key)){Loader.assemblies[key] = assembly;}
		var instance = assembly.CreateInstance(classPath);
		if(instance != null){
			if(!Loader.instances.ContainsKey(classPath)){Loader.instances[classPath] = [];}
			Loader.instances[classPath].Add(instance);
		}
		return instance;
	}
	public static void Link(Assembly assembly,string fieldPath,object value){
		var separator = fieldPath.LastIndexOf(".");
		var fieldName = fieldPath.Substring(separator+1);
		var parentPath = fieldPath.Remove(separator);
		var parent = assembly.GetType(parentPath);
		parent.GetField(fieldName).SetValue(null,value);
	}
}