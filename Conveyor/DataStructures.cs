using System;
using UdpKit;
using System.Collections.Generic;

namespace Conveyor 
{
	public class ServerFunctions
	{
		public Action<int> MessageSerializerIsNotRegistered;
		public Action<int> MessageHandlerIsNotRegistered;
		public Func<UdpEndPoint, bool> AcceptConnection;
		public Action<Connection> ClientConnected;
		public Action<Connection> ClientConnectionLost;
		public Func<bool> ServerRequiresAuthentication;
		public Action<Connection, Message> ClientJoined;
		public Func<Connection, bool> ServerHasAvailablePlayerSlots;
		public Action<Connection> ClientDisconnected;
		public Action<Connection> ClientReconnected;
		public Func<Connection, Message, bool> IsAuthenticationRequest;
		public Func<Connection, Message, bool> IsValidAuthentication;
		public Func<Connection, bool> MaxAuthorizationAttemptsExceeded;
		public Action<Connection> ClientFailedToAuthenticate;

		public Func<Connection, int, Message> CreateUnknownMessageMessage;
		public Func<Connection, int, Message> CreateMalformedMessageMessage;
		public Func<Connection, Message, Message> CreateConnectedMessage;
		public Func<Connection, Message, Message> CreateJoinedServerMessage;
		public Func<Connection, Message, Message> CreateServerFullMessage;
		public Func<Connection, Message, Message> CreateAuthenticationRequiredMessage;
		public Func<Connection, Message, Message> CreateInvalidAuthenticationMessage;
	}

	public class ClientFunctions
	{
		public Action<UdpEndPoint> ConnectedToServer;
		public Action<UdpEndPoint> LostConnectionToServer;
		public Action<UdpEndPoint> FailedToConnectToServer;
		public Action<UdpEndPoint> ConnectionRefusedByServer;
		public Func<int, Message> CreateUnknownMessageMessage;
		public Func<int, Message> CreateMalformedMessageMessage;
		public Action<int> MessageSerializerIsNotRegistered;
		public Action<int> MessageHandlerIsNotRegistered;
	}

	public struct ServerMessageHandler
	{
		public int Id;
		public Action<Connection, Message> Handler;
		public bool RequiresAuthentication;
		public Func<bool> Active;
		public bool DontClearOnSceneLoad;

		public ServerMessageHandler(int id, Action<Connection, Message> handler) : this(id, handler, true, () => true, false) {}
		public ServerMessageHandler(int id, Action<Connection, Message> handler, bool requiresAuthentication) : this(id, handler, requiresAuthentication, () => true, false) {}
		public ServerMessageHandler(int id, Action<Connection, Message> handler, bool requiresAuthentication, Func<bool> active, bool dontClearOnSceneLoad)
		{
			Id = id;
			Handler = handler;
			RequiresAuthentication = requiresAuthentication;
			Active = active;
			DontClearOnSceneLoad = dontClearOnSceneLoad;
		}
	}

	public struct ClientMessageHandler
	{
		public int Id;
		public Action<Message> Handler;
		public Func<bool> Active;
		public bool DontClearOnSceneLoad;

		public ClientMessageHandler(int id, Action<Message> handler) : this(id, handler, () => true, false) {}
		public ClientMessageHandler(int id, Action<Message> handler, Func<bool> active) : this(id, handler, active, false) {}
		public ClientMessageHandler(int id, Action<Message> handler, Func<bool> active, bool dontClearOnSceneLoad)
		{
			Id = id;
			Handler = handler;
			Active = active;
			DontClearOnSceneLoad = dontClearOnSceneLoad;
		}
	}

	#region Internal classes / structs ===========================================

	// used only by server
	class PendingMessage
	{
		public readonly IEnumerable<Connection> Recipients;
		public readonly Message Message;

		public PendingMessage(IEnumerable<Connection> recipients, Message message)
		{
			Recipients = recipients;
			Message = message;
		}

		public PendingMessage(Connection recipient, Message message) : this(new List<Connection> { recipient }, message) { }
	}

	struct DeserializedMessage 
	{
		public int Id;
		public Maybe<IMessageData> Data;
		public int OriginalId;
	}

	static class NetworkConnectionUtils
	{
		internal static UdpSocket CreatePlatformSpecificSocket<TSerializer>() where TSerializer : UdpSerializer, new() 
		{
			return CreatePlatformSpecificSocket<TSerializer>(new UdpConfig());
		}

		internal static UdpSocket CreatePlatformSpecificSocket<TSerializer>(UdpConfig config) where TSerializer : UdpSerializer, new() 
		{
			#if UNITY_EDITOR || UNITY_STANDALONE || UNITY_WEBPLAYER
			return UdpSocket.Create<UdpPlatformManaged, TSerializer>(config);
			#elif UNITY_IPHONE
			return UdpSocket.Create<UdpPlatformIOS, TSerializer>(config);
			#elif UNITY_ANDROID
			return UdpSocket.Create<UdpPlatformAndroid, TSerializer>(config);
			#else
			throw new System.NotImplementedException ("UdpKit doesn't support the current platform");
			#endif
		}
	}
	#endregion
}