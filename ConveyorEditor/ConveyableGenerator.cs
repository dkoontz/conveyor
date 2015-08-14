using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Avro.IO;

namespace Conveyor
{
	public static class PartialClassGenerator
	{
		public const string REQUIRED_IMPORTS = "using System;\nusing System.IO;\nusing System.Collections.Generic;\nusing Avro.IO;";

		// Generates the implementation for a Conveyable partial class.
		// The input type must have the [Conveyable] attribute and one or more
		// public properties with a getter and private setter.
		//
		// Conveyor will generate the constuctor as well as WithXYZ functions
		// that create a new copy of the type with one of the fields changed.
		// 
		// Conveyor also generates a Serialize and Deserialize static function
		// pair to be used when writing to / reading from a byte[]
		public static string Generate(Type type)
		{
			if (!type.GetCustomAttributes(false).Any(a => a is ConveyableAttribute))
			{
				throw new ArgumentException(string.Format("Class does not have [{0}] attribute", typeof(ConveyableAttribute)));
			}

			var properties = type
				.GetProperties(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
				.ToArray();

			if (properties.Length == 0)
			{
				throw new ArgumentException(string.Format("Class {0} does not have any public Properties with public getter and private setter", type.FullName));
			}

			var invalidProperties = properties
				.Where(p => 
					!p.CanRead
					|| !p.GetGetMethod(true).IsPublic
					|| !p.CanWrite
					|| !p.GetSetMethod(true).IsPrivate)
				.ToArray();

			if (invalidProperties.Any())
			{
				throw new ArgumentException(
					string.Format(
						"Class {0} contains the following invalid properties: [{1}]. Properties must have a public getter and private setter", 
						type.FullName, 
						string.Join(", ", invalidProperties.Select(p => p.Name).ToArray())));
			}

			var output = new StringBuilder();
			Action<string> appendWithTwoTabs = str => output.AppendLine("\t\t" + str);

			output.AppendLine(string.Format("namespace {0}", type.Namespace));
			output.AppendLine("{"); {
				output.AppendLine(string.Format("\tpublic partial class {0}", type.Name));
				output.AppendLine("\t{");
//				GenerateSchema(properties, appendWithTwoTabs);
				GenerateConstructor(type.Name, properties, appendWithTwoTabs);
				GenerateModifierFunctions(type.Name, properties, appendWithTwoTabs);
				GenerateSerializerAndDeserializer(type.Name, properties, appendWithTwoTabs);
				output.AppendLine("\t}");
			} output.AppendLine("}");

			return output.ToString();
		}

//		static void GenerateSchema(PropertyInfo[] properties, Action<string> writer)
//		{
//			writer("public static readonly Dictionary<string, Type> SCHEMA = ");
//			writer("\tnew Dictionary<string, Type>");
//			writer("\t{");
//
//			foreach (var property in properties)
//			{
//				writer(string.Format("\t\t{{ \"{0}\", typeof({1}) }},", property.Name, property.PropertyType));
//			}
//			writer("\t};");
//		}

		static void GenerateConstructor(string typeName, PropertyInfo[] properties, Action<string> writer)
		{
			var propertyParameters = 
				properties.Select(p =>
					{
						return IsList(p.PropertyType) 
							? string.Format("List<{0}> {1}", listElementType(p.PropertyType).FullName.Replace('+', '.'), p.Name) 
							: string.Format("{0} {1}", p.PropertyType.FullName.Replace('+', '.'), p.Name);
					})
					.ToArray();

			writer("");
			writer(string.Format("public {0}({1})", typeName, string.Join(", ", propertyParameters)));

			writer("{");
			foreach (var property in properties)
			{
				writer(string.Format("\tthis.{0} = {0};", property.Name));
			}

			writer("}");
		}

		static void GenerateModifierFunctions(string typeName, PropertyInfo[] properties, Action<string> writer)
		{
			var propertyNames = properties.Select(p => p.Name).ToArray();
			for (var i = 0; i < properties.Length; ++i)
			{
				var property = properties[i];

				writer("");
				writer(
					string.Format(
						"public {0} With{1}({2} newValue)", 
						typeName, 
						property.Name, 
						IsList(property.PropertyType) 
						? string.Format("List<{0}>", listElementType(property.PropertyType).FullName.Replace('+', '.')) 
						: property.PropertyType.FullName.Replace('+', '.')
					));
				writer("{");

				var parameters = string.Join(
					", ", 
					propertyNames.Select(
						name => 
						Array.IndexOf(propertyNames, name) == i 
						? "newValue"
						: name)
					.ToArray());
				writer(string.Format("\treturn new {0}({1});", typeName, parameters));
				writer("}");
			}
		}

		static readonly Func<Type, Type> arrayElementType = t => t.GetElementType();
		static readonly Func<Type, Type> listElementType = t => t.GetGenericArguments()[0];
		static void GenerateSerializerAndDeserializer(string className, PropertyInfo[] properties, Action<string> writer)
		{
			const string localObjectName = "obj";
			const string localDecoderName = "decoder";
			const string localEncoderName = "encoder";

			writer("");
			writer(string.Format("public static void Serialize({0} {1}, BinaryEncoder {2})", className, localObjectName, localEncoderName));
			writer("{");

			foreach (var property in properties)
			{
				writer("");
				var type = property.PropertyType;
				var propertyName = localObjectName + "." + property.Name;

				if (type.IsArray) 
				{
					WriteCollection(writer, type, propertyName, localEncoderName, "Length", arrayElementType);
				}
				else if (IsList(type))
				{
					WriteCollection(writer, type, propertyName, localEncoderName, "Count", listElementType);
				}
				else
				{
					writer(string.Format("\t{0};", TypeToWriter(type, propertyName, localEncoderName)));
				}
			}

			writer("}");

			writer("");
			writer(string.Format("public static {0} Deserialize(BinaryDecoder {1})", className, localDecoderName));
			writer("{");

			for (var i = 0; i < properties.Length; ++i)
			{
				var property = properties[i];
				var type = property.PropertyType;

				writer("");

				if (type.IsArray)
				{
					writer(string.Format("\t{0} property{1};", type.FullName.Replace('+', '.'), i));
					writer("\t{");
					ReadCollection(
						writer, 
						type, 
						localDecoderName,
						(indexName, reader) => string.Format("[{0}] = {1}", indexName, reader),
						(t, length) => 
						{
							var fullName = t.FullName.Replace('+', '.');
							return string.Format(
								"new {0}{1}]", 
								fullName.Substring(0, fullName.Length - 1), 
								length);
						},
						arrayElementType);
					writer(string.Format("\tproperty{0} = collection;", i));
					writer("\t}");
				}
				else if (IsList(type))
				{
					writer(string.Format("\tList<{0}> property{1};", listElementType(type).FullName.Replace('+', '.'), i));
					writer("\t{");
					ReadCollection(
						writer, 
						type,
						localDecoderName,
						(indexName, reader) => string.Format(".Add({0})", reader),
						(t, length) => string.Format("new List<{0}>({1})", listElementType(t).FullName.Replace('+', '.'), length), 
						listElementType);
					writer(string.Format("\tproperty{0} = collection;", i));
					writer("\t}");
					//				ReadCollection(writer, type, propertyName, localEncoderName, "Count", t => t.GetGenericArguments()[0]);
				}
				else
				{
					writer(string.Format("\t{0} property{1} = {2};", type.FullName.Replace('+', '.'), i, TypeToReader(type, localDecoderName)));
				}
			}

			writer("");
			writer(string.Format("\treturn new {0}(", className));
			for (var i = 0; i < properties.Length; ++i)
			{
				writer(
					string.Format(
						"\t\tproperty{0}{1}", 
						i, 
						i < properties.Length - 1
						? "," 
						: ");"));
			}
			writer("}");
		}

		static string TypeToReader(Type type, string decoderName)
		{
			if (typeof(bool) == type)
			{
				return string.Format("{0}.ReadBoolean()", decoderName);
			}
			else if (typeof(byte[]) == type)
			{
				return string.Format("{0}.ReadBytes()", decoderName);
			}
			else if (typeof(double) == type)
			{
				return string.Format("{0}.ReadDouble()", decoderName);
			}
			else if (typeof(Enum).IsAssignableFrom(type))
			{
				return string.Format("({0}){1}.ReadEnum()", type.FullName.Replace('+', '.'), decoderName);
			}
			else if (typeof(float) == type)
			{
				return string.Format("{0}.ReadFloat()", decoderName);
			}
			else if (typeof(int) == type)
			{
				return string.Format("{0}.ReadInt()", decoderName);
			}
			else if (typeof(long) == type)
			{
				return string.Format("{0}.ReadLong()", decoderName);
			}
			else if (typeof(string) == type)
			{
				return string.Format("{0}.ReadString()", decoderName);
			}
			else if (type.GetCustomAttributes(false).Any(attr => attr.GetType().IsAssignableFrom(typeof(ConveyableAttribute))))
			{
				return string.Format("{0}.Deserialize({1})", type.Name, decoderName);
			}
			else 
			{
				var deserializer = GetDeserializerFor(type);
				if (deserializer != null) 
				{
					Console.WriteLine("found deserializer: " + deserializer.Name);
					return string.Format("{0}.{1}({2})", deserializer.DeclaringType.FullName, deserializer.Name, decoderName);
				}
				else
				{
					throw new NotImplementedException(string.Format("No serializers of the type \"public static {0} Deserializer(BinaryDecoder decoder)\" could be found, perhaps you are missing the [{1}] attribute?", type, typeof(ConveyorDeserializerAttribute).Name));
				}
			}
		}

		static string TypeToWriter(Type type, string property, string encoderName)
		{
			if (typeof(bool) == type)
			{
				return string.Format("{0}.WriteBoolean({1})", encoderName, property);
			}
			else if (typeof(byte[]) == type)
			{
				return string.Format("{0}.WriteBytes({1})", encoderName, property);
			}
			else if (typeof(double) == type)
			{
				return string.Format("{0}.WriteDouble({1})", encoderName, property);
			}
			else if (typeof(Enum).IsAssignableFrom(type))
			{
				return string.Format("{0}.WriteEnum((int){1})", encoderName, property);
			}
			else if (typeof(float) == type)
			{
				return string.Format("{0}.WriteFloat({1})", encoderName, property);
			}
			else if (typeof(int) == type)
			{
				return string.Format("{0}.WriteInt({1})", encoderName, property);
			}
			else if (typeof(long) == type)
			{
				return string.Format("{0}.WriteLong({1})", encoderName, property);
			}
			else if (typeof(string) == type)
			{
				return string.Format("{0}.WriteString({1})", encoderName, property);
			}
			else if (type.GetCustomAttributes(false).Any(attr => attr.GetType().IsAssignableFrom(typeof(ConveyableAttribute))))
			{
				return string.Format("{0}.Serialize({1}, {2})", type.Name, property, encoderName);
			}
			else 
			{
				var serializer = GetSerializerFor(type);
				if (serializer != null) 
				{
					Console.WriteLine("found serializer: " + serializer.Name);
					return string.Format("{0}.{1}({2}, {3})", serializer.DeclaringType.FullName, serializer.Name, property, encoderName);
				}
				else
				{
					throw new NotImplementedException(string.Format("No serializer of the type \"public static void Serializer({0} data, BinaryEncoder encoder)\" could be found, perhaps you are missing the [{1}] attribute?", type, typeof(ConveyorSerializerAttribute).Name));
				}
			}
		}

		static MethodInfo GetDeserializerFor(Type type)
		{
			var deserializers = AppDomain.CurrentDomain
				.GetAssemblies()
				.SelectMany(
					assembly => assembly
					.GetTypes()
					.SelectMany(t => t.GetMethods(BindingFlags.Static | BindingFlags.Public))
					.Where(m => m.GetCustomAttributes(false).Any(a => a is ConveyorDeserializerAttribute)));

			return deserializers.FirstOrDefault(m => 
				m.ReturnParameter.ParameterType == type
				&& m.GetParameters().Length == 1
				&& m.GetParameters()[0].ParameterType == typeof(BinaryDecoder));
		}

		static MethodInfo GetSerializerFor(Type type)
		{
			var deserializers = AppDomain.CurrentDomain
				.GetAssemblies()
				.SelectMany(
					assembly => assembly
					.GetTypes()
					.SelectMany(t => t.GetMethods(BindingFlags.Static | BindingFlags.Public))
					.Where(m => m.GetCustomAttributes(false).Any(a => a is ConveyorSerializerAttribute)));

			return deserializers.FirstOrDefault(m => 
				m.ReturnParameter.ParameterType == typeof(void)
				&& m.GetParameters().Length == 2
				&& m.GetParameters()[0].ParameterType == type
				&& m.GetParameters()[1].ParameterType == typeof(BinaryEncoder));
		}

		static bool IsList(Type type)
		{
			return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>);
		}

		static void WriteCollection(Action<string> writer, Type type, string propertyName, string localEncoderName, string lengthPropertyName, Func<Type, Type> elementType)
		{
			writer(string.Format("\t{0}.WriteInt({1}.{2});", localEncoderName, propertyName, lengthPropertyName));
			writer(string.Format("\tfor (var i = 0; i < {0}.{1}; ++i)", propertyName, lengthPropertyName));
			writer("\t{");
			writer(string.Format("\t\t{0};", TypeToWriter(elementType(type), propertyName + "[i]", localEncoderName)));
			writer("\t}");
		}

		static void ReadCollection(Action<string> writer, Type type, string localDecoderName, Func<string, string, string> addToCollection, Func<Type, string, string> collectionConstructor, Func<Type, Type> elementType)
		{
			writer(string.Format("\tvar length = {0}.ReadInt();", localDecoderName));
			writer(string.Format("\tvar collection = {0};", collectionConstructor(type, "length")));
			writer("\tfor (var i = 0; i < length; ++i)");
			writer("\t{");
			writer(string.Format("\t\tcollection{0};", addToCollection("i", TypeToReader(elementType(type), localDecoderName))));
			writer("\t}");
		}
	}
}