#define CONVEYOR_DEBUG

using System;
using UdpKit;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Conveyor
{
	public class ServerNetworkConnection 
	{
		public Guid ServerConnectionId { get { return serverConnection.Id; } }

		readonly ServerFunctions functions;
		readonly Queue<PendingMessage> pendingMessages;
		readonly Dictionary<int, MessageSerializers> serializers;
		readonly Dictionary<int, ServerMessageHandler> serverMessageHandlers;
		readonly Dictionary<int, ClientMessageHandler> clientMessageHandlers;
		readonly Dictionary<UdpConnection, Connection> connections;
		readonly Dictionary<Guid, UdpConnection> idToUdp;

		// TODO: implement some sort of aging policy that allows these to be discarded
		// probably need a timestamp in here and then periodically a check to flush
		// old ones
		readonly Dictionary<Connection, ConnectionStatus> disconnectedClients;

		UdpSocket socket;
		Connection serverConnection;

		public ServerNetworkConnection(
			ServerFunctions functions, 
			Dictionary<int, MessageSerializers> serializers, 
			List<ServerMessageHandler> serverMessageHandlers, 
			List<ClientMessageHandler> clientMessageHandlers)
		{
			var missingFunctions = FindMissingFunctions(functions);
			if (missingFunctions.Length > 0)
			{
				throw new ArgumentException(string.Format("Invalid server functions, missing [{0}]", string.Join(", ", missingFunctions)));
			}

			this.functions = functions;
			this.serializers = serializers;

			this.serverMessageHandlers = new Dictionary<int, ServerMessageHandler>();
			foreach (var handler in serverMessageHandlers)
			{
				this.serverMessageHandlers[handler.Id] = handler;
			}

			this.clientMessageHandlers = new Dictionary<int, ClientMessageHandler>();
			foreach (var handler in clientMessageHandlers)
			{
				this.clientMessageHandlers[handler.Id] = handler;
			}

			pendingMessages = new Queue<PendingMessage>();
			connections = new Dictionary<UdpConnection, Connection>();
			idToUdp = new Dictionary<Guid, UdpConnection>();
			disconnectedClients = new Dictionary<Connection, ConnectionStatus>();

			#if CONVEYOR_DEBUG
			Debug.Log(
				string.Format("Registering server handlers: {0}\nRegistering client handlersL {1}",  
					(serverMessageHandlers.Count == 0 
						? "No server handlers"
						: string.Join(", ", this.serverMessageHandlers.Keys.Select(k => k.ToString()).ToArray())),
					(clientMessageHandlers.Count == 0 
						? "No client handlers"
						: string.Join(", ", this.clientMessageHandlers.Keys.Select(k => k.ToString()).ToArray()))));
					
			#endif
		}

		public void Start(string ipAddress, ushort port) {
			var config = new UdpConfig();
			config.AutoAcceptIncommingConnections = false;

			socket = NetworkConnectionUtils.CreatePlatformSpecificSocket<ConveyorSerializer>(config);
			socket.Start(new UdpEndPoint(UdpIPv4Address.Parse(ipAddress), port));
			socket.UserToken = serializers;
		}

		public void Stop()
		{
			socket.Close();

			connections.Clear();
			disconnectedClients.Clear();
			idToUdp.Clear();
			serverConnection = null;
		}

		public void Poll()
		{
			UdpEvent evt;
			while (socket.Poll(out evt)) 
			{
				switch (evt.EventType) 
				{
					case UdpEventType.SocketStarted:
						#if CONVEYOR_DEBUG
						Debug.Log("Socket started");
						#endif

						serverConnection = new Connection();
						idToUdp[serverConnection.Id] = null;
						functions.ClientConnected(serverConnection);
						ProcessClientJoining(serverConnection, new Message(ClientToServerMessages.Login, null));
						break;
					case UdpEventType.SocketStartupFailed:
						Debug.LogError("Socket failed to start");
						break;
					case UdpEventType.ConnectRequest:
						if (functions.AcceptConnection(evt.EndPoint))
						{
							socket.Accept(evt.EndPoint);
						}
						else 
						{
							socket.Refuse(evt.EndPoint);
						}
						break;
					case UdpEventType.Connected:
						{
							var connection = new Connection();
							connections[evt.Connection] = connection;
							idToUdp[connection.Id] = evt.Connection;
							
							functions.ClientConnected(connection);

							if (!functions.ServerRequiresAuthentication())
							{
								#if CONVEYOR_DEBUG
								Debug.Log("Auto joining client");
								#endif

								ProcessClientJoiningIfOpenSlots(connection, new Message(ClientToServerMessages.Login, null));
							}
						}

						break;
					case UdpEventType.Disconnected:
						{
							Connection connection;
							if (connections.TryGetValue(evt.Connection, out connection))
							{
								disconnectedClients.Add(connection, connection.Status);
								connection.Status = ConnectionStatus.ConnectionLost;
								connections.Remove(evt.Connection);
								idToUdp.Remove(connection.Id);

								functions.ClientConnectionLost(connection);
							}
						}
						break;
					case UdpEventType.ObjectSendFailed:
						//						Debug.Log("object send failed");
						break;
					case UdpEventType.ObjectRejected:
						//						Debug.Log("object was rejected: " + evt.Object);
						break;
					case UdpEventType.ObjectDelivered:
						//						Debug.Log("object delivered");
						break;
					case UdpEventType.ObjectLost:
						//						Debug.Log("object lost");
						break;
					case UdpEventType.ObjectReceived:
						Debug.Log("Object received from: " + evt.EndPoint.Address);
						{
							Connection connection;
							if (!connections.TryGetValue(evt.Connection, out connection))
							{
								evt.Connection.Disconnect();
							}

							var deserializedMessage = (DeserializedMessage)evt.Object;

							if (deserializedMessage.Id == ServerToClientMessages.InvalidMessage)
							{
								SendUnknownMessage(connection, deserializedMessage.OriginalId);
							}
							else if (!deserializedMessage.Data.HasValue)
							{
								SendMalformedMessage(connection, deserializedMessage.Id);
							}
							else
							{
								var message = 
									new Message
									{
										Id = deserializedMessage.Id,
										Data = deserializedMessage.Data.Value
									};

								if (message.Id == ClientToServerMessages.Reconnect)
								{
									var data = (ReconnectMessage)message.Data;
									var oldConnection = disconnectedClients.Keys.FirstOrDefault(c => c.Id == data.OriginalId);
									if (oldConnection == null)
									{
										evt.Connection.Disconnect();
									}
									else
									{
										oldConnection.Status = disconnectedClients[oldConnection];
										idToUdp[oldConnection.Id] = evt.Connection;
										connections.Add(evt.Connection, oldConnection);
										disconnectedClients.Remove(oldConnection);
										functions.ClientReconnected(oldConnection);
									}
								}
								else if (message.Id == ClientToServerMessages.Disconnect)
								{
									disconnectedClients.Add(connection, connection.Status);
									connection.Status = ConnectionStatus.Disconnected;
									connections.Remove(evt.Connection);
									idToUdp.Remove(connection.Id);

									functions.ClientDisconnected(connection);
								}
								else if (functions.IsAuthenticationRequest(connection, message))
								{
									if (functions.ServerRequiresAuthentication())
									{
										if (connection.Status == ConnectionStatus.Connected)
										{
											if (functions.IsValidAuthentication(connection, message))
											{
												ProcessClientJoiningIfOpenSlots(connection, message);
											}
											else
											{
												if (functions.MaxAuthorizationAttemptsExceeded(connection))
												{
													evt.Connection.Disconnect();
												}
												else
												{
													SendInvalidAuthorization(connection, message);
													functions.ClientFailedToAuthenticate(connection);
												}
											}
										}
									}
								}
								else
								{
									ProcessMessage(message, serverMessageHandlers, clientMessageHandlers, connection, functions);
								}
							}
						}
						break;
					case UdpEventType.ObjectSent:
//						Debug.Log("object sent");
						break;
					default:
						throw new NotImplementedException(evt.EventType.ToString());
				}
			}
		}

		public void SendPendingMessages()
		{
			while (pendingMessages.Count > 0)
			{
				var message = pendingMessages.Dequeue();

				if (!serializers.ContainsKey(message.Message.Id))
				{
					functions.MessageSerializerIsNotRegistered(message.Message.Id);
				}

				foreach (var connection in message.Recipients)
				{
					var udpConnection = idToUdp[connection.Id];
					if (udpConnection == null)
					{
						ProcessMessageFromSelf(message.Message);
					}
					else
					{
						udpConnection.Send(message.Message);
					}
				}
			}
		}

		public void QueueMessage(Connection recipient, Message message)
		{
			QueueMessage(new List<Connection> { recipient }, message);
		}

		public void QueueMessage(IEnumerable<Connection> recipients, Message message)
		{
			pendingMessages.Enqueue(new PendingMessage(recipients, message));
		}

		void ProcessMessageFromSelf(Message message) 
		{
			ProcessMessage(message, serverMessageHandlers, clientMessageHandlers, serverConnection, functions);
		}

		bool Authorized(Connection connection) 
		{
			var authIsNotRequired = !functions.ServerRequiresAuthentication();
			var alreadyAuthenticated = connection.Status == ConnectionStatus.Joined;

			return authIsNotRequired || alreadyAuthenticated;
		}

		void SendUnknownMessage(Connection connection, int messageId) 
		{
			QueueMessage(connection, functions.CreateUnknownMessageMessage(connection, messageId));
		}

		void SendMalformedMessage(Connection connection, int messageId) 
		{
			QueueMessage(connection, functions.CreateMalformedMessageMessage(connection, messageId));
		}

		void SendServerFull(Connection connection, Message message) 
		{
			QueueMessage(connection, functions.CreateServerFullMessage(connection, message));
		}

		void SendJoinedServer(Connection connection, Message message) 
		{
			QueueMessage(connection, functions.CreateJoinedServerMessage(connection, message));
		}

		void SendInvalidAuthorization(Connection connection, Message message) 
		{
			QueueMessage(connection, functions.CreateInvalidAuthenticationMessage(connection, message));
		}

		void SendAuthenticationRequired(Connection connection, Message message) 
		{
			QueueMessage(connection, functions.CreateAuthenticationRequiredMessage(connection, message));
		}
			
		void ProcessClientJoiningIfOpenSlots(Connection connection, Message message) 
		{
			if (!functions.ServerHasAvailablePlayerSlots(connection)) 
			{
				SendServerFull(connection, message);
			}
			else
			{
				ProcessClientJoining(connection, message);
			}
		}

		void ProcessMessage(
			Message message, 
			Dictionary<int, ServerMessageHandler> serverHandlers, 
			Dictionary<int, ClientMessageHandler> clientHandlers,
			Connection connection, 
			ServerFunctions functions) 
		{
			if (connection.Id == ServerConnectionId)
			{
				ServerMessageHandler serverHandler;
				if (serverHandlers.TryGetValue(message.Id, out serverHandler))
				{
					ProcessHandler(serverHandler, connection, message);
				}
				else
				{
					ClientMessageHandler clientHandler;
					if (clientHandlers.TryGetValue(message.Id, out clientHandler))
					{
						ProcessHandler(clientHandler, connection, message);
					}
					else
					{
						functions.MessageHandlerIsNotRegistered(message.Id);
					}
				}
			}
			else // came from external client
			{
				ServerMessageHandler serverHandler;
				if (!serverHandlers.TryGetValue(message.Id, out serverHandler))
				{
					functions.MessageHandlerIsNotRegistered(message.Id);
				}
				else
				{
					if (!serverHandler.RequiresAuthentication || Authorized(connection)) 
					{
						ProcessHandler(serverHandler, connection, message);
					}
					else 
					{
						SendAuthenticationRequired(connection, message);
					}
				}
			}
		}

		void ProcessClientJoining(Connection connection, Message message)
		{
			connection.Status = ConnectionStatus.Joined;
			functions.ClientJoined(connection, message);
			SendJoinedServer(connection, message);
		}

		static void ProcessHandler(ServerMessageHandler messageHandler, Connection connection, Message message) {
			if (messageHandler.Active())
			{
				messageHandler.Handler(connection, message);
			}
		}

		static void ProcessHandler(ClientMessageHandler messageHandler, Connection connection, Message message) {
			if (messageHandler.Active())
			{
				messageHandler.Handler(message);
			}
		}

		static string[] FindMissingFunctions(ServerFunctions functions) {
			return new List<string>
			{
				functions.MessageSerializerIsNotRegistered == null ? "MessageSerializerIsNotRegistered" : null,
				functions.MessageHandlerIsNotRegistered == null ? "MessageHandlerIsNotRegistered" : null,
				functions.AcceptConnection == null ? "AcceptConnection" : null,
				functions.ClientConnected == null ? "ClientConnected" : null,
				functions.ClientConnectionLost == null ? "ClientConnectionLost" : null,
				functions.ServerRequiresAuthentication == null ? "ServerRequiresAuthorization" : null,
				functions.ClientJoined == null ? "ClientJoined" : null,
				functions.ServerHasAvailablePlayerSlots == null ? "ServerHasAvailablePlayerSlots" : null,
				functions.ClientDisconnected == null ? "ClientDisconnected" : null,
				functions.ClientReconnected == null ? "ClientReconnected" : null,
				functions.IsAuthenticationRequest == null ? "IsAuthorizationRequest" : null,
				functions.IsValidAuthentication == null ? "IsValidAuthorization" : null,
				functions.MaxAuthorizationAttemptsExceeded == null ? "MaxAuthorizationAttemptsExceeded" : null,
				functions.ClientFailedToAuthenticate == null ? "ClientFailedToAuthenticate" : null,

				functions.CreateUnknownMessageMessage == null ? "CreateUnknownMessageMessage" : null,
				functions.CreateMalformedMessageMessage == null ? "CreateMalformedMessageMessage" : null,
				functions.CreateConnectedMessage == null ? "CreateConnectedMessage" : null,
				functions.CreateJoinedServerMessage == null ? "CreateJoinedServerMessage" : null,
				functions.CreateServerFullMessage == null ? "CreateServerFullMessage" : null,
				functions.CreateAuthenticationRequiredMessage == null ? "CreateAuthenticationRequiredMessage" : null,
				functions.CreateInvalidAuthenticationMessage == null ? "CreateInvalidAuthenticationMessage" : null,
			}
			.Where(str => !string.IsNullOrEmpty(str))
			.ToArray();
		}
	}
}