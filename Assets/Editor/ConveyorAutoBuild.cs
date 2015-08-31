using UnityEngine;
using UnityEditor;
using System.IO;
using System;
using System.Linq;
using System.Collections.Generic;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Conveyor
{
	class AutoBuildConfig
	{
		public string Messages { get; set; }
		public string Generated { get; set; }
		public string Namespace { get; set; }
	}

	public class ConveyorAutoBuild : AssetPostprocessor  
	{
		const string AUTO_BUILD_CONFIG = "conveyor_auto_generate_config.yml";

		static void OnPostprocessAllAssets(
			string[] importedAssets,
			string[] deletedAssets,
			string[] movedAssets,
			string[] movedFromAssetPaths) 
		{
			var config = GetConfig();

			CreateDirectoriesIfMissing(config.Messages, config.Generated);

			var messagePathWithAssets = "Assets/" + config.Messages;
			var generatedPathWithAssets = "Assets/" + config.Generated;

			var fullMessagePath = Application.dataPath + "/" + config.Messages;
			var fullGeneratedPath = Application.dataPath + "/" + config.Generated;

			foreach (string str in importedAssets)
			{
				string[] splitStr = str.Split('/');

				string folder = string.Join("/", splitStr.Take(splitStr.Length - 1).ToArray());
				string fileName = splitStr.Last();
//				Debug.Log("folder: " + folder);
//				Debug.Log("full:   " + fullMessagePath);
				if (folder == messagePathWithAssets)
				{
					var messageDefinitionsArray = Directory
						.GetFiles(messagePathWithAssets, "*.yml")
						.Select(f => f.Replace("Assets/", ""))
						.Select(
							file => {
								try
								{
									var messageDefinitionFile = string.Format ("{0}/{1}", Application.dataPath, file);
									var reader = new StreamReader(messageDefinitionFile);
									var deserializer = new Deserializer(namingConvention: new UnderscoredNamingConvention());
									var definitions = deserializer.Deserialize<UnprocessedMessageDefinitions>(reader);
									definitions.OriginalFileModifiedTime = File.GetLastWriteTimeUtc(messageDefinitionFile);

									return Either<Pair<string, Exception>, UnprocessedMessageDefinitions>.Right(definitions);
								}
								catch (Exception ex)
								{
									return Either<Pair<string, Exception>, UnprocessedMessageDefinitions>.Left(
										new Pair<string, Exception>(file, ex));
								}
							})
						.Where(e => e.IsLeft || (e.IsRight && e.RightValue != null))
						.ToArray();

					if (messageDefinitionsArray.Any(e => e.IsLeft))
					{
						Debug.LogError(
							"Error parsing the following message files: " + 
							string.Join(
								", ", 
								messageDefinitionsArray
									.Where(e => e.IsLeft)
									.Select(e => e.LeftValue.First + "\n" + e.LeftValue.Second.Message + "\n" + e.LeftValue.Second.StackTrace)
									.ToArray()));
					}
					else
					{
						var messageDefinitions = 
							new MessageDefinitions
							{
								Types = messageDefinitionsArray
									.Where(e => e.RightValue.Types != null)
									.SelectMany(e => ProcessMessageDefinition(e.RightValue.Types, e.RightValue.OriginalFileModifiedTime))
									.ToArray(),
								EnumTypes = messageDefinitionsArray
									.Where(e => e.RightValue.EnumTypes != null)
									.SelectMany(e => e.RightValue.EnumTypes)
									.ToArray(),
								CustomSerializers = messageDefinitionsArray
									.Where(e => e.RightValue.CustomSerializers != null)
									.SelectMany(e => e.RightValue.CustomSerializers)
									.ToArray()
							};


						foreach (var type in messageDefinitions.Types)
						{
							var outputFile = string.Format("{0}/{1}.cs", generatedPathWithAssets, type.Name);

							if (File.GetLastWriteTimeUtc(outputFile) < type.DefinitionModifiedTime)
							{
								try
								{
									var generatedCode = ClassGenerator.Generate(type, messageDefinitions, config.Namespace);
									
									File.WriteAllText(
										outputFile,
										string.Format(
											"{0}{1}\n\n{2}",
											ClassGenerator.REQUIRED_IMPORTS,
											string.Join(
												"\n",
												(type.AdditionalImports ?? new string[0])
												.Select(t => string.Format("using {0};", t))
												.ToArray()),
											generatedCode));
									Debug.Log("Generated class for " + type.Name);
								}
								catch (Exception ex)
								{
									Debug.LogError(
										string.Format(
											"Failed to generate file for {0}\n{1}\n{2}", 
											type, 
											ex.Message, 
											ex.StackTrace));
								}
							}
						}
					}

//					var sourceFile = fullMessagePath + "/" + fileName;
//					var destinationFile = fullGeneratedPath + "/" + fileName;
//					Debug.Log("Directory: " + folder);
//					Debug.Log("File name: " + fileName);
//					Debug.Log("source file: " + File.GetLastWriteTimeUtc(sourceFile));
//					Debug.Log("dest   file: " + File.GetLastWriteTimeUtc(destinationFile));
//					Debug.Log("difference: " + File.GetLastWriteTimeUtc(sourceFile).CompareTo(File.GetLastWriteTimeUtc(destinationFile)));
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

		static AutoBuildConfig GetConfig()
		{
			var configFilePath = string.Format("{0}/{1}", Application.dataPath, AUTO_BUILD_CONFIG);
			return !File.Exists(configFilePath)
				? CreateDefaultConfig(configFilePath)
				: ReadExistingConfig(configFilePath);
		}

		static AutoBuildConfig CreateDefaultConfig(string configFilePath)
		{
			var config = new AutoBuildConfig 
			{
				Messages = "Network/Messages",
				Generated = "Network/Generated",
				Namespace = "YourNamespace"
			};

			var serializer = new Serializer(namingConvention: new CamelCaseNamingConvention());
			using (var writer = new StreamWriter(configFilePath))
			{
				serializer.Serialize(writer, config, typeof(AutoBuildConfig));
			}
			AssetDatabase.Refresh();
			return config;
		}

		static AutoBuildConfig ReadExistingConfig (string configFilePath)
		{
			var reader = new StreamReader(configFilePath);
			var deserializer = new Deserializer(namingConvention: new CamelCaseNamingConvention());
			return deserializer.Deserialize<AutoBuildConfig>(reader);
		}

		static Field StringToField(string fieldString)
		{
			var parts = fieldString.Split(' ');
			return new Field
			{
				Type = parts[0],
				Name = parts[1]
			};
		}

		static MessageDefinition[] ProcessMessageDefinition(UnprocessedMessageDefinition[] unproccessedDefinitions, DateTime originalModifiedTime)
		{
			return unproccessedDefinitions
				.Select(
					definition =>
						new MessageDefinition
						{
							Name = definition.Name,
							Fields = definition
								.Fields
								.Select(f => StringToField(f))
								.ToArray(),
							AdditionalImports = definition.AdditionalImports,
							DefinitionModifiedTime = originalModifiedTime,
						}
					)
				.ToArray();
		}
	}
}