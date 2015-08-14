using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Conveyor
{
	internal static class MessageUtils
	{
		public static object ValidateFields(Type type)
		{
			var fieldValues = type
				.GetFields(BindingFlags.Public | BindingFlags.Static)
				.Where(f => f.Name != "ValidIds")
				.Select(f => new Pair<string, object>(f.Name, f.GetValue(typeof(ServerToClientMessages))))
				.ToArray();

			var nonIntFields = fieldValues.Where(f => !(f.Second is int)).ToArray();
			if (nonIntFields.Length > 0)
			{
				return string.Format(
					"Invalid fields \"{0}\", fields must be public const int", 
					string.Join(", ", nonIntFields.Select(pair => pair.First).ToArray()));
			}

			var valueCount = new Dictionary<int, int>();
			foreach(var value in fieldValues)
			{
				var key = (int)value.Second;
				if (!valueCount.ContainsKey(key))
				{
					valueCount[key] = 0;
				}

				++valueCount[key];
			}

			var duplicateIds = valueCount.Where(kvp => kvp.Value > 1).ToArray();
			if (duplicateIds.Length > 0)
			{
				return string.Format(
					"The value(s) \"{0}\" are duplicated, each field must have a unique value", 
					string.Join(", ", duplicateIds.Select(pair => pair.Key.ToString()).ToArray()));
			}

			return new HashSet<int>(fieldValues.Select(pair => (int)pair.Second));
		}
	}

	public static partial class ServerToClientMessages
	{
		public static HashSet<int> ValidIds { get; internal set; }

		public const int InvalidMessage = 0;
		public const int UnknownMessage = 1;
		public const int MalformedMessage = 2;
		public const int Connected = 3;

		public const int ServerFull = 4;
		public const int AuthenticationRequired = 5;
		public const int InvalidAuthentication = 6;
		public const int JoinedServer = 7;

		public const int ServerConfiguration = 8;
	}

	public static partial class ClientToServerMessages
	{
		public static HashSet<int> ValidIds { get; internal set; }

		public const int Disconnect = 100;
		public const int Reconnect = 101;
		public const int GetServerConfiguration = 102;
		public const int Login = 103;
	}
}