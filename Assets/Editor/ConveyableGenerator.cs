using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Avro.IO;
using UnityEditor;
using System.IO;
using UnityEngine;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Conveyor
{
	internal class UnprocessedMessageDefinitions
	{
		public UnprocessedMessageDefinition[] Types { get; set; }
		public string[] EnumTypes { get; set; }
		public CustomSerializer[] CustomSerializers { get; set; }
		public DateTime OriginalFileModifiedTime;
	}

	internal class UnprocessedMessageDefinition
	{
		public string Name { get; set; }
		public string[] Fields { get; set; }
		public string[] AdditionalImports { get; set; }
	}

	public class CustomSerializer
	{
		public string Type { get; set; }
		public string Serializer { get; set; }
		public string Deserializer { get; set; }
	}

	public class MessageDefinitions
	{
		public MessageDefinition[] Types { get; set; }
		public string[] EnumTypes { get; set; }
		public CustomSerializer[] CustomSerializers { get; set; }
	}

	public class MessageDefinition
	{
		public string Name;
		public Field[] Fields;
		public string[] AdditionalImports;
		public DateTime DefinitionModifiedTime;
	}

	public class Field
	{
		public string Type;
		public string Name;
	}

	public static class ClassGenerator
	{
		public const string REQUIRED_IMPORTS = "using System;\nusing System.IO;\nusing System.Collections.Generic;\nusing Avro.IO;\nusing Conveyor;\n";

		[MenuItem("Assets/Create/Conveyor Message", false, 10)]
		public static void CreateMessageFile()
		{
			var path = "Assets";
			foreach (UnityEngine.Object obj in Selection.GetFiltered(typeof(UnityEngine.Object), SelectionMode.Assets))
			{
				path = AssetDatabase.GetAssetPath(obj);
				if (File.Exists(path))
				{
					path = Path.GetDirectoryName(path);
				}
				break;
			}

			using (var f = File.CreateText(AssetDatabase.GenerateUniqueAssetPath(path + "/Messages.yml")))
			{
				f.WriteLine ("types:");
				f.WriteLine ("\t-");
				f.WriteLine ("\t\tname: Message1Name");
				f.WriteLine ("\t\tfields:");
				f.WriteLine ("\t\t\t- type1 Field1Name");
				f.WriteLine ("\t\t\t- type2 Field2Name");
				f.WriteLine ("\t\tadditional_imports:");
				f.WriteLine ("\t\t\t- SomeNameSpace");
				f.WriteLine ("\t-");
				f.WriteLine ("\t\tname: Message2Name");
				f.WriteLine ("\t\tfields:");
				f.WriteLine ("\t\t\t- type3 Field1Name");
				f.WriteLine ("enum_types:");
				f.WriteLine ("\t- FirstEnumType");
				f.WriteLine ("\t- SecondEnumType");
				f.WriteLine ("custom_serializers:");
				f.WriteLine ("\t-");
				f.WriteLine ("\t\ttype: FooType");
				f.WriteLine ("\t\tserializer: NamespaceIfDifferent.ClassName.Serializer");
				f.WriteLine ("\t\tdeserializer: NamespaceIfDifferent.ClassName.Deserializer");
			}

			AssetDatabase.Refresh();
		}

		// Generates the implementation of a Conveyable class.
		//
		// Conveyor will generate the constuctor as well as WithXYZ functions
		// that create a new copy of the type with one of the fields changed.
		//
		// Conveyor also generates a Serialize and Deserialize static function
		// pair to be used when writing to / reading from a byte[]
		public static string Generate(MessageDefinition type, MessageDefinitions definitions, string classNamespace)
		{
			var output = new StringBuilder();
			Action<string> appendWithTwoTabs = str => output.AppendLine("\t\t" + str);

			output.AppendLine(string.Format("namespace {0}", classNamespace));
			output.AppendLine("{");
			output.AppendLine(string.Format("\tpublic class {0} : {1}", type.Name, typeof(IConveyable).Name));
			output.AppendLine("\t{");
			GenerateFields(type.Fields, appendWithTwoTabs);
			GenerateConstructor(type.Name, type.Fields, definitions, appendWithTwoTabs);
			GenerateModifierFunctions(type.Name, type.Fields, definitions, appendWithTwoTabs);
			GenerateSerializerAndDeserializer(type.Name, type.Fields, definitions, appendWithTwoTabs);
			output.AppendLine("\t}");
			output.AppendLine("}");

			return output.ToString();
		}

		static void GenerateFields(Field[] fields, Action<string> writer)
		{
			writer("");
			foreach (var field in fields)
			{
				writer(string.Format("public readonly {0} {1};", field.Type, field.Name));
			}
		}

		static void GenerateConstructor(string typeName, Field[] fields, MessageDefinitions definitions, Action<string> writer)
		{
			writer("");
			writer(
				string.Format(
					"public {0}({1})", 
					typeName, 
					string.Join(
						", ", 
						fields
							.Select(field => string.Format("{0} {1}", field.Type, field.Name))
							.ToArray())));

			writer("{");
			foreach (var field in fields)
			{
				writer(string.Format("\tthis.{0} = {0};", field.Name));
			}

			writer("}");
		}

		static void GenerateModifierFunctions(string typeName, Field[] fields, MessageDefinitions definitions, Action<string> writer)
		{
			var fieldNames = fields.Select(f => f.Name).ToArray();
			for (var i = 0; i < fields.Length; ++i)
			{
				var field = fields[i];

				writer("");
				writer(
					string.Format(
						"public {0} With{1}({2} newValue)",
						typeName,
						field.Name,
						field.Type
					));
				writer("{");

				var parameters = string.Join(
					", ",
					fieldNames.Select(
						name =>
						Array.IndexOf(fieldNames, name) == i
						? "newValue"
						: name)
					.ToArray());
				writer(string.Format("\treturn new {0}({1});", typeName, parameters));
				writer("}");
			}
		}

		static void GenerateSerializerAndDeserializer(string className, Field[] fields, MessageDefinitions definitions, Action<string> writer)
		{
			const string localObjectName = "obj";
			const string localDecoderName = "decoder";
			const string localEncoderName = "encoder";

			writer("");
			writer("public static MessageSerializers Serializers");
			writer("{");
			writer("\tget");
			writer("\t{");
			writer("\t\treturn new MessageSerializers(");
			writer("\t\t\tSerialize,");
			writer("\t\t\tDeserialize);");
			writer("\t}");
			writer("}");
			writer("");

			writer(string.Format("public static void Serialize({0} data, BinaryEncoder {1})", typeof(IConveyable).Name, localEncoderName));
			writer("{");
			writer(string.Format("\t{0} {1} = ({0})data;", className, localObjectName));

			foreach (var field in fields)
			{
				writer("");
				if (IsArray(field.Type))
				{
					WriteCollection(
						writer, 
						field.Type, 
						localObjectName + "." + field.Name, 
						localEncoderName, 
						"Length", 
						ArrayElementType(field.Type),
						definitions.Types, 
						definitions.EnumTypes, 
						definitions.CustomSerializers);
				}
				else if (IsList(field.Type))
				{
					WriteCollection(
						writer, 
						field.Type, 
						localObjectName + "." + field.Name, 
						localEncoderName, 
						"Count", 
						ListElementType(field.Type),
						definitions.Types, 
						definitions.EnumTypes, 
						definitions.CustomSerializers);
				}
				else
				{
					writer(
						string.Format(
							"\t{0};", 
							TypeToWriter(
								field.Type, 
								localObjectName + "." + field.Name, 
								localEncoderName, 
								definitions.Types, 
								definitions.EnumTypes, 
								definitions.CustomSerializers)));
				}
			}

			writer("}");

			writer("");
			writer(string.Format("public static {0} Deserialize(BinaryDecoder {1})", className, localDecoderName));
			writer("{");

			for (var i = 0; i < fields.Length; ++i)
			{
				var field = fields[i];
				writer("");

				if (IsArray(field.Type))
				{
					writer(string.Format("\t{0} field{1};", field.Type, i));
					writer("\t{");
					ReadCollection(
						str => writer("\t" + str),
						field.Type,
						localDecoderName,
						(indexName, reader) => string.Format("[{0}] = {1}", indexName, reader),
						(t, length) => string.Format("new {0}{1}]", t.Substring(0, t.Length - 1), length),
						ArrayElementType(field.Type),
						definitions.Types, 
						definitions.EnumTypes, 
						definitions.CustomSerializers);
					writer(string.Format("\t\tfield{0} = collection;", i));
					writer("\t}");
				}
				else if (IsList(field.Type))
				{
					writer(string.Format("\t{0} field{1};", field.Type, i));
					writer("\t{");
					ReadCollection(
						str => writer("\t" + str),
						field.Type,
						localDecoderName,
						(indexName, reader) => string.Format(".Add({0})", reader),
						(t, length) => string.Format("new {0}({1})", t, length),
						ListElementType(field.Type),
						definitions.Types, 
						definitions.EnumTypes, 
						definitions.CustomSerializers);
					writer(string.Format("\t\tfield{0} = collection;", i));
					writer("\t}");
				}
				else
				{
					writer(
						string.Format(
							"\t{0} field{1} = {2};", 
							field.Type, 
							i, 
							TypeToReader(
								field.Type, 
								localDecoderName,
								definitions.Types, 
								definitions.EnumTypes, 
								definitions.CustomSerializers)));
				}
			}

			writer("");
			writer(string.Format("\treturn new {0}(", className));
			for (var i = 0; i < fields.Length; ++i)
			{
				writer(
					string.Format(
						"\t\tfield{0}{1}",
						i,
						i < fields.Length - 1
						? ","
						: ");"));
			}
			writer("}");
		}

		static string TypeToReader(
			string type, 
			string decoderName,
			MessageDefinition[] messages,
			string[] enumTypes, 
			CustomSerializer[] customSerializers)
		{
			if ("bool" == type)
			{
				return string.Format("{0}.ReadBoolean()", decoderName);
			}
			else if ("double" == type)
			{
				return string.Format("{0}.ReadDouble()", decoderName);
			}
			else if (enumTypes.Contains(type))
			{
				return string.Format("({0}){1}.ReadEnum()", type, decoderName);
			}
			else if ("float" == type)
			{
				return string.Format("{0}.ReadFloat()", decoderName);
			}
			else if ("int" == type)
			{
				return string.Format("{0}.ReadInt()", decoderName);
			}
			else if ("long" == type)
			{
				return string.Format("{0}.ReadLong()", decoderName);
			}
			else if ("string" == type)
			{
				return string.Format("{0}.ReadString()", decoderName);
			}
			else if (messages.Any(m => m.Name == type))
			{
				return string.Format("{0}.Deserialize({1})", type, decoderName);
			}
			else
			{
				var custom = customSerializers.FirstOrDefault(s => s.Type == type);
				if (custom != null)
				{
					Console.WriteLine("found deserializer: " + custom.Deserializer);
					return string.Format("{0}({1})", custom.Deserializer, decoderName);
				}
				else
				{
					throw new NotImplementedException(string.Format("No serializers of the type \"public static {0} Deserializer(BinaryDecoder decoder)\" could be found", type));
				}
			}
		}

		static string TypeToWriter(
			string type, 
			string field, 
			string encoderName, 
			MessageDefinition[] messages,
			string[] enumTypes, 
			CustomSerializer[] customSerializers)
		{
			if ("bool" == type)
			{
				return string.Format("{0}.WriteBoolean({1})", encoderName, field);
			}
			else if ("double" == type)
			{
				return string.Format("{0}.WriteDouble({1})", encoderName, field);
			}
			else if (enumTypes.Contains(type))
			{
				return string.Format("{0}.WriteEnum((int){1})", encoderName, field);
			}
			else if ("float" == type)
			{
				return string.Format("{0}.WriteFloat({1})", encoderName, field);
			}
			else if ("int" == type)
			{
				return string.Format("{0}.WriteInt({1})", encoderName, field);
			}
			else if ("long" == type)
			{
				return string.Format("{0}.WriteLong({1})", encoderName, field);
			}
			else if ("string" == type)
			{
				return string.Format("{0}.WriteString({1})", encoderName, field);
			}
			else if (messages.Any(m => m.Name == type))
			{
				return string.Format("{0}.Serialize({1}, {2})", type, field, encoderName);
			}
			else
			{
				var custom = customSerializers.FirstOrDefault(s => s.Type == type);
				if (custom != null)
				{
					Console.WriteLine("found serializer: " + custom.Serializer);
					return string.Format("{0}({1}, {2})", custom.Serializer, field, encoderName);
				}
				else
				{
					throw new NotImplementedException(string.Format("No serializer of the type \"public static void Serializer({0} data, BinaryEncoder encoder)\" was registered", type));
				}
			}
		}
//
//		static MethodInfo GetDeserializerFor(Type type)
//		{
//			var deserializers = AppDomain.CurrentDomain
//				.GetAssemblies()
//				.SelectMany(
//					assembly => assembly
//					.GetTypes()
//					.SelectMany(t => t.GetMethods(BindingFlags.Static | BindingFlags.Public))
//					.Where(m => m.GetCustomAttributes(false).Any(a => a is ConveyorDeserializerAttribute)));
//
//			return deserializers.FirstOrDefault(m =>
//				m.ReturnParameter.ParameterType == type
//				&& m.GetParameters().Length == 1
//				&& m.GetParameters()[0].ParameterType == typeof(BinaryDecoder));
//		}
//
//		static MethodInfo GetSerializerFor(Type type)
//		{
//			var deserializers = AppDomain.CurrentDomain
//				.GetAssemblies()
//				.SelectMany(
//					assembly => assembly
//					.GetTypes()
//					.SelectMany(t => t.GetMethods(BindingFlags.Static | BindingFlags.Public))
//					.Where(m => m.GetCustomAttributes(false).Any(a => a is ConveyorSerializerAttribute)));
//
//			return deserializers.FirstOrDefault(m =>
//				m.ReturnParameter.ParameterType == typeof(void)
//				&& m.GetParameters().Length == 2
//				&& m.GetParameters()[0].ParameterType == type
//				&& m.GetParameters()[1].ParameterType == typeof(BinaryEncoder));
//		}

		public static bool IsArray(string typeName)
		{
			return typeName.EndsWith("]");
		}

		public static bool IsList(string typeName)
		{
			return typeName.Contains("List<");
		}

		public static string ArrayElementType(string type)
		{
			var bracketIndex = type.IndexOf("[");
			return type.Substring(0, bracketIndex);
		}

		public static string ListElementType (string type)
		{
			var angleBracketIndex = type.IndexOf("<");
			return type.Substring(angleBracketIndex + 1, type.Length - angleBracketIndex - 2);
		}

		static void WriteCollection(
			Action<string> writer, 
			string typeName, 
			string fieldName, 
			string localEncoderName, 
			string lengthPropertyName, 
			string elementType,
			MessageDefinition[] messages,
			string[] enumTypes, 
			CustomSerializer[] customSerializers)
		{
			writer(string.Format("\t{0}.WriteInt({1}.{2});", localEncoderName, fieldName, lengthPropertyName));
			writer(string.Format("\tfor (var i = 0; i < {0}.{1}; ++i)", fieldName, lengthPropertyName));
			writer("\t{");
			writer(string.Format("\t\t{0};", TypeToWriter(elementType, fieldName + "[i]", localEncoderName, messages, enumTypes, customSerializers)));
			writer("\t}");
		}

		static void ReadCollection(
			Action<string> writer, 
			string type, 
			string localDecoderName, 
			Func<string, string, string> addToCollection, 
			Func<string, string, string> collectionConstructor, 
			string elementType, 
			MessageDefinition[] messages,
			string[] enumTypes, 
			CustomSerializer[] customSerializers)
		{
			writer(string.Format("\tvar length = {0}.ReadInt();", localDecoderName));
			writer(string.Format("\tvar collection = {0};", collectionConstructor(type, "length")));
			writer("\tfor (var i = 0; i < length; ++i)");
			writer("\t{");
			writer(string.Format("\t\tcollection{0};", addToCollection("i", TypeToReader(elementType, localDecoderName, messages, enumTypes, customSerializers))));
			writer("\t}");
		}
	}
}
