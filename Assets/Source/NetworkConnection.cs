using UnityEngine;

using System;
using UdpKit;
using System.Collections.Generic;
using System.Linq;

namespace Conveyor 
{
	public enum LogType
	{
		Info,
		Warning,
		Error
	}

	public enum RecipientType
	{
		All,
		Server,
		AllButServer,
	}

	// This class is a reference implementation for using ClientNetworkConnection
	// and ServerNetworkConnection. You can implement your own version based on
	// this one or from scratch. If you like the way connection data is stored 
	// in the Connections List, you can simply add new serializers and handlers.
	//
	// NetworkConnection provides a default implementation of everything needed
	// to connect as a client or host as a server. NetworkConnection presents a 
	// unified interface for sending message as either the client or server and 
	// can switch between acting as client or server.
	[System.Serializable]
	public class NetworkConnection
	{
		public const ushort DEFAULT_PORT = 44444;

		public bool AcceptConnections;
		public bool PasswordRequired;
		public string Password;
		public int MaxPlayerSlots;

		[ReadOnly] public List<Connection> Connections;

		public ServerFunctions ServerFunctions;
		public ClientFunctions ClientFunctions;
		public Dictionary<int, MessageSerializers> Serializers;
		public List<ServerMessageHandler> ServerMessageHandlers;
		public List<ClientMessageHandler> ClientMessageHandlers;

		ServerNetworkConnection server;
		ClientNetworkConnection client;

		Action<LogType, string> logger;

		// This constructor is necessary so Unity can serialize/deserialize
		// for the inspector
		public NetworkConnection() { }

		public NetworkConnection(bool useDefaults, Action<LogType, string> logger)
		{
			CheckMessageIds();
			this.logger = logger;

			logger(LogType.Info, "Creating new network");
			Connections = new List<Connection>();

			if (useDefaults)
			{
				logger(LogType.Info, "setting up default handlers");
				ServerFunctions = DefaultServerFunctions();
				ClientFunctions = DefaultClientFunctions();
				Serializers = BuiltInMessageSerializers.DefaultSerializers;
				ServerMessageHandlers = DefaultServerHandlers();
				ClientMessageHandlers = DefaultClientHandlers();
			}
		}

		public void Poll()
		{
			if (server != null)
			{
				server.Poll();
				server.SendPendingMessages();
			}
			else if (client != null)
			{
				client.Poll();
				client.SendPendingMessages();
			}
		}

		public void StartServer()
		{
			StartServer(ServerFunctions, Serializers, ServerMessageHandlers, ClientMessageHandlers);
		}

		public void StartServer(
			ServerFunctions functions, 
			Dictionary<int, MessageSerializers> serializers, 
			List<ServerMessageHandler> serverMessageHandlers,
			List<ClientMessageHandler> clientMessageHandlers)
		{
			StartServer(functions, serializers, serverMessageHandlers, clientMessageHandlers, Network.player.ipAddress, DEFAULT_PORT);
		}

		public void StartServer(
			ServerFunctions functions, 
			Dictionary<int, MessageSerializers> serializers, 
			List<ServerMessageHandler> serverMessageHandlers,
			List<ClientMessageHandler> clientMessageHandlers,
			string ipAddress,
			ushort port)
		{
			StopExistingClientAndServer();
			server = new ServerNetworkConnection(functions, serializers, serverMessageHandlers, clientMessageHandlers, logger);
			server.Start(ipAddress, port);
			logger(LogType.Info, "Started server");
		}

		public void StartClient(string ipAddress, ushort port)
		{
			StartClient(ClientFunctions, Serializers, ClientMessageHandlers, ipAddress, port);
		}

		public void StartClient(
			ClientFunctions functions, 
			Dictionary<int, MessageSerializers> serializers, 
			List<ClientMessageHandler> messageHandlers,
			string ipAddress,
			ushort port)
		{
			StopExistingClientAndServer();
			client = new ClientNetworkConnection(functions, serializers, messageHandlers, logger);
			client.Connect(ipAddress, port);
			logger(LogType.Info, "Started client");
		}

		public void QueueMessage(IEnumerable<Connection> recipients, Message message)
		{
			if (server != null)
			{
				server.QueueMessage(recipients, message);
			}
			else if (client != null)
			{
				throw new ArgumentException("Client connections cannot send to other clients", "recipients");
			}
			else
			{
				throw new InvalidOperationException("Cannot send a message until started as Server");
			}
		}

		public void QueueMessage(Connection recipient, Message message)
		{
			if (server != null)
			{
				server.QueueMessage(recipient, message);
			}
			else if (client != null)
			{
				throw new ArgumentException("Client connections cannot send to other clients", "recipient");
			}
			else
			{
				throw new InvalidOperationException("Cannot send a message until started as Server");
			}
		}

		public void QueueMessageForServer(Message message)
		{
			if (server != null)
			{
				server.QueueMessage(Connections.Find(c => c.Id == server.ServerConnectionId), message);
			}
			else if (client != null)
			{
				client.QueueMessage(message);
			}
			else
			{
				throw new InvalidOperationException("Cannot send a message until started as Server or Client");
			}
		}


		public void ReplaceHandler(int messageId, Action<Message> handler)
		{
			ReplaceHandler(messageId, handler, () => true);
		}

		public void ReplaceHandler(int messageId, Action<Message> handler, Func<bool> active)
		{
			ReplaceHandler(messageId, handler, active, false);
		}

		public void ReplaceHandler(int messageId, Action<Message> handler, Func<bool> active, bool dontClearOnSceneLoad)
		{
			ClientMessageHandlers.RemoveAll(h => h.Id == messageId);
			ClientMessageHandlers.Add(
				new ClientMessageHandler(messageId, handler, active, dontClearOnSceneLoad));
		}

		public void ReplaceHandler(int messageId, Action<Connection, Message> handler)
		{
			ReplaceHandler(messageId, handler, true, () => true);
		}

		public void ReplaceHandler(int messageId, Action<Connection, Message> handler, bool requiresAuthentication, Func<bool> active)
		{
			ReplaceHandler(messageId, handler, requiresAuthentication, active, false);
		}

		public void ReplaceHandler(int messageId, Action<Connection, Message> handler, bool requiresAuthentication, Func<bool> active, bool dontClearOnSceneLoad)
		{
			ServerMessageHandlers.RemoveAll(h => h.Id == messageId);
			ServerMessageHandlers.Add(
				new ServerMessageHandler(messageId, handler, requiresAuthentication, active, dontClearOnSceneLoad));
		}

		public void LevelWasLoaded()
		{
			ClientMessageHandlers.RemoveAll(handler => !handler.DontClearOnSceneLoad);
			ServerMessageHandlers.RemoveAll(handler => !handler.DontClearOnSceneLoad);
		}

		public bool IsServerConnection(Connection connection)
		{
			if (server == null)
			{
				throw new InvalidOperationException("Server is not started");
			}
			return connection.Id == server.ServerConnectionId;
		}



		void StopExistingClientAndServer() 
		{
			if (client != null) 
			{
				client.Disconnect();
				client = null;
			}

			if (server != null) 
			{
				server.Stop();
				server = null;
			}

			Connections.Clear();
		}

		ServerFunctions DefaultServerFunctions() 
		{
			return new ServerFunctions
			{
				AcceptConnection = conn => AcceptConnections,
				ServerRequiresAuthentication = () => PasswordRequired,
				ClientConnected = conn => 
				{
					Connections.Add(conn);
					logger(LogType.Info, "Client connected: " + conn.Id);
				},
				ClientDisconnected = conn => 
				{
					Connections.Remove(conn);
					logger(LogType.Info, "Client disconnected: " + conn.Id);
				},
				ClientConnectionLost = conn => 
				{
					Connections.Remove(conn);
					logger(LogType.Info, "Lost connection to client: " + conn.Id);
				},
				ClientJoined = (conn, msg) => 
				{
					logger(LogType.Info, "Client joined server: " + conn.Id);
				},
				ClientReconnected = conn => 
				{
					Connections.Add(conn);
					logger(LogType.Info, "Client reconnected: " + conn.Id);
				},
				ClientFailedToAuthenticate = conn => {},
				ServerHasAvailablePlayerSlots = conn => Connections.Count(c => c.Status == ConnectionStatus.Joined) < MaxPlayerSlots,
				IsAuthenticationRequest = (conn, msg) => msg.Id == ClientToServerMessages.Login,
				IsValidAuthentication = (conn, msg) => (msg.Data as LoginMessage).Authentication == Password,
				MaxAuthorizationAttemptsExceeded = conn => false,
				CreateMalformedMessageMessage = (conn, id) => new Message(ServerToClientMessages.MalformedMessage, new MalformedMessageMessage(id)),
				CreateUnknownMessageMessage = (conn, id) => new Message(ServerToClientMessages.UnknownMessage, new UnknownMessageMessage(id)),
				CreateServerFullMessage = (conn, msg) => new Message(ServerToClientMessages.ServerFull, null),
				CreateAuthenticationRequiredMessage = (conn, msg) => new Message(ServerToClientMessages.AuthenticationRequired, null),
				CreateInvalidAuthenticationMessage = (conn, msg) => new Message(ServerToClientMessages.InvalidAuthentication, null),
				CreateJoinedServerMessage = (conn, msg) => new Message(ServerToClientMessages.JoinedServer, new JoinedServerMessage(null)),
				CreateConnectedMessage = (conn, msg) => new Message(ServerToClientMessages.Connected, new ConnectedMessage(conn.Id)),
				MessageHandlerIsNotRegistered = id => logger(LogType.Error, "No handler registered for message: " + id),
				MessageSerializerIsNotRegistered = id => logger(LogType.Error, "No serializer registered for message: " + id),
			};
		}

		ClientFunctions DefaultClientFunctions() 
		{
			return new ClientFunctions
			{
				ConnectedToServer = endpoint => logger(LogType.Info, "Connected to server: " + endpoint.Address),
				ConnectionRefusedByServer = endpoint => logger(LogType.Warning, "Connected refused by server: " + endpoint.Address + ":" + endpoint.Port),
				FailedToConnectToServer = endpoint => logger(LogType.Warning, "Failed to connect to server: " + endpoint.Address + ":" + endpoint.Port),
				LostConnectionToServer = endpoint => logger(LogType.Info, "Lost connection to server: " + endpoint.Address),
				CreateMalformedMessageMessage = id => new Message(ServerToClientMessages.MalformedMessage, new MalformedMessageMessage(id)),
				CreateUnknownMessageMessage = id => new Message(ServerToClientMessages.UnknownMessage, new UnknownMessageMessage(id)),
				MessageHandlerIsNotRegistered = id => logger(LogType.Error, "No handler registered for message: " + id),
				MessageSerializerIsNotRegistered = id => logger(LogType.Error, "No serializer registered for message: " + id),
			};
		}

		List<ServerMessageHandler> DefaultServerHandlers() 
		{
			return new List<ServerMessageHandler>();
		}

		List<ClientMessageHandler> DefaultClientHandlers()
		{
			return new List<ClientMessageHandler>
			{
				new ClientMessageHandler(
					ServerToClientMessages.JoinedServer, 
					msg => logger(LogType.Info, "Joined server")),
				new ClientMessageHandler(
					ServerToClientMessages.ServerFull,
					msg => logger(LogType.Info, "Server is Full")),
				new ClientMessageHandler(
					ServerToClientMessages.AuthenticationRequired,
					msg => logger(LogType.Info, "Authentication required")),
				new ClientMessageHandler(
					ServerToClientMessages.InvalidAuthentication,
					msg => logger(LogType.Info, "Invalid authentication")),
				new ClientMessageHandler(
					ServerToClientMessages.MalformedMessage,
					msg => logger(LogType.Error, string.Format("Server received message with id: {0} that was malformed", (msg.Data as MalformedMessageMessage).MessageId))),
				new ClientMessageHandler(
					ServerToClientMessages.UnknownMessage,
					msg => logger(LogType.Error, string.Format("Server received message with id: {0} that was unknown", (msg.Data as UnknownMessageMessage).MessageId))),
			};
		}

		static void CheckMessageIds()
		{
			var serverToClientIds = MessageUtils.ValidateFields(typeof(ServerToClientMessages));
			if (serverToClientIds is string)
			{
				throw new InvalidProgramException(serverToClientIds as string);
			}
			else
			{
				ServerToClientMessages.ValidIds = serverToClientIds as HashSet<int>;
			}

			var clientToServerIds = MessageUtils.ValidateFields(typeof(ClientToServerMessages));
			if (clientToServerIds is string)
			{
				throw new InvalidProgramException(clientToServerIds as string);
			}
			else
			{
				ClientToServerMessages.ValidIds = clientToServerIds as HashSet<int>;
			}
		}
	}
}