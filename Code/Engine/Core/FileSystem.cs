namespace Engine.Core;
using System;
using System.IO;
public static class FileSystem{
	public static string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
	public static string contentDirectory = AppDomain.CurrentDomain.BaseDirectory+"Content\\";
	public static void Start() => Directory.SetCurrentDirectory(FileSystem.contentDirectory);
}