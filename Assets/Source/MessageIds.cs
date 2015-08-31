using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Conveyor
{
	public static class BuiltInServerToClientMessages
	{
		public const int InvalidMessage = 200;
		public const int UnknownMessage = 201;
		public const int MalformedMessage = 202;
		public const int Connected = 203;

		public const int ServerFull = 204;
		public const int AuthenticationRequired = 205;
		public const int InvalidAuthentication = 206;
		public const int JoinedServer = 207;

		public const int ServerConfiguration = 208;
	}

	public static class BuiltInClientToServerMessages
	{
		public const int Disconnect = 220;
		public const int Reconnect = 221;
		public const int GetServerConfiguration = 222;
		public const int Login = 223;
	}
}