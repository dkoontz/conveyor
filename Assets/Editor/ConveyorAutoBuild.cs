using UnityEngine;
using UnityEditor;
using System.IO;
using System;
using System.Linq;
using System.Collections.Generic;

namespace Conveyor
{
	public class ConveyorAutoBuild : AssetPostprocessor  
	{
		static void OnPostprocessAllAssets(
			string[] importedAssets,
			string[] deletedAssets,
			string[] movedAssets,
			string[] movedFromAssetPaths) 
		{
			var paths = GetPaths();
			var messagePath = paths.First;
			var generatedPath = paths.Second;

			CreateDirectoriesIfMissing(messagePath, generatedPath);

			var messagePathWithAssets = "Assets/" + messagePath;
//			var generatedPathWithAssets = "Assets/" + generatedPath;

			var fullMessagePath = Application.dataPath + "/" + messagePath;
			var fullGeneratedPath = Application.dataPath + "/" + generatedPath;

			var filesWithErrors = new List<string>();
			foreach (string str in importedAssets)
			{
//				Debug.Log("Reimported Asset: " + str);
				string[] splitStr = str.Split('/');
				
				string folder = string.Join("/", splitStr.Take(splitStr.Length - 1).ToArray());
				string fileName = splitStr.Last();
//				string extension = splitStr[2];

//				Debug.Log("folder: " + folder);
//				Debug.Log("full:   " + fullMessagePath);
				if (folder == messagePathWithAssets)
				{
					var sourceFile = fullMessagePath + "/" + fileName;
					var destinationFile = fullGeneratedPath + "/" + fileName;
//					Debug.Log("Directory: " + folder);
//					Debug.Log("File name: " + fileName);
//					Debug.Log("source file: " + File.GetLastWriteTimeUtc(sourceFile));
//					Debug.Log("dest   file: " + File.GetLastWriteTimeUtc(destinationFile));
//					Debug.Log("difference: " + File.GetLastWriteTimeUtc(sourceFile).CompareTo(File.GetLastWriteTimeUtc(destinationFile)));
					var error = BuildUtils.GenerateFileContents(sourceFile, destinationFile, "Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null");
					if (!string.IsNullOrEmpty(error))
					{
						filesWithErrors.Add(error);
					}
				}
//				Debug.Log("File type: " + extension);             
				// for each file in messages, check if there is a file in generated that is older or non-existant
//				if (folder)
			}

			if (filesWithErrors.Count > 0)
			{
				Debug.LogError("There were errors generating some files");
				foreach (var file in filesWithErrors)
				{
					Debug.LogError(file);
				}
			}

			AssetDatabase.Refresh();

//			foreach (string str in deletedAssets)
//				Debug.Log("Deleted Asset: " + str);
				// remove generated file is deleted file is on the message path
//			
//			for (int i=0;i<movedAssets.Length;i++)
//				Debug.Log("Moved Asset: " + movedAssets[i] + " from: " + movedFromAssetPaths[i]);
		}

		static void CreateDirectoriesIfMissing(string messagePath, string generatedPath)
		{
			var fullMessagePath = string.Format("{0}/{1}", Application.dataPath, messagePath);
			var fullGeneratedPath = string.Format("{0}/{1}", Application.dataPath, generatedPath);
			if (!Directory.Exists(fullMessagePath))
			{
				Directory.CreateDirectory(fullMessagePath);
			}
			if (!Directory.Exists(fullGeneratedPath))
			{
				Directory.CreateDirectory(fullGeneratedPath);
			}
		}

		static Pair<string, string> GetPaths()
		{
			var configFilePath = Application.dataPath + "/conveyor_auto_generate_config.txt";
			string[] lines =
				{
					"Messages:Network/Messages",
					"Generated:Network/GeneratedMessages"
				};

			if (!File.Exists(configFilePath))
			{
				using (var writer = new StreamWriter(configFilePath))
				{
					writer.WriteLine(lines[0]);
					writer.WriteLine(lines[1]);
					writer.Close();
				}
			}
			else
			{
				lines = File.ReadAllLines(configFilePath);
			}

			if (lines.Length < 2)
			{
				throw new Exception(BuildUtils.MALFORMED_ERROR_MESSAGE);
			}

			try
			{
				var source = lines[0].Split(':')[1];
				var destination = lines[1].Split(':')[1];
				return new Pair<string, string>(source, destination);
			}
			catch
			{
				throw new Exception(BuildUtils.MALFORMED_ERROR_MESSAGE);
			}
		}
	}
}