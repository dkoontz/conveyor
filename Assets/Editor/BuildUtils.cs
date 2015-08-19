using System;
using System.Text.RegularExpressions;
using System.IO;
using System.Linq;

namespace Conveyor
{
	public static class BuildUtils
	{
		internal const string MALFORMED_ERROR_MESSAGE = "Config file is malformed, should contain 2 lines starting with \"Messages:<path/within/Assets>\" and \"Generated:Assets/<path/within/Assets>\"";
		static readonly Regex NAMESPACE_MATCH = new Regex("\\s*namespace\\s(\\w+).*");
		static readonly Regex CLASS_MATCH = new Regex(".*\\s+class\\s+(\\w+).*");


		public static string GenerateFileContents(string sourceFile, string destinationFile, string assemblyName)
		{
			if (
				!File.Exists(destinationFile)
				|| File.GetLastWriteTimeUtc(sourceFile).CompareTo(File.GetLastWriteTimeUtc(destinationFile)) > 0)
			{
				var namespaceMatch = NAMESPACE_MATCH
					.Match(
						File
							.ReadAllLines(sourceFile)
							.FirstOrDefault(l => l.Contains("namespace")));

				if (!namespaceMatch.Success)
				{
					return null;
				}

				var namespacePrefix = namespaceMatch.Groups.Count < 2
					? ""
					: namespaceMatch.Groups[1] + ".";

				var classNameMatch = CLASS_MATCH
					.Match(
						File.ReadAllLines(sourceFile)
						.First(l => l.Contains("class")));

				var className = classNameMatch.Groups[1];

				var t = Type.GetType(
					string.Format(
						"{0}{1}, {2}",
						namespacePrefix,
						className,
						assemblyName));

				string generatedCode;
				try
				{
					generatedCode = PartialClassGenerator.Generate(t);

					File.WriteAllText(
						destinationFile,
						string.Format(
							"{0}\n\n{1}",
							PartialClassGenerator.REQUIRED_IMPORTS,
							generatedCode));

				}
				catch (NotImplementedException ex)
				{
					return string.Format("{0}\n{1}", sourceFile, ex.Message);
				}
			}

			return null;
		}
	}
}
