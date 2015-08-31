using System;
using UdpKit;
using System.Collections.Generic;
using System.Linq;

namespace Conveyor
{
	public class ClientNetworkConnection
	{
		readonly ClientFunctions functions;
		readonly Queue<Message> pendingMessages;
		readonly Dictionary<int, MessageSerializers> serializers;
		readonly Dictionary<int, ClientMessageHandler> messageHandlers;
		UdpSocket socket;
		UdpConnection server;
		Action<LogType, string> logger;

		public ClientNetworkConnection(ClientFunctions functions, Dictionary<int, MessageSerializers> serializers, List<ClientMessageHandler> messageHandlers, Action<LogType, string> logger)
		{
			var missingFunctions = FindMissingFunctions(functions);
			if (missingFunctions.Length > 0)
			{
				throw new ArgumentException(string.Format("Invalid server functions, missing [{0}]", string.Join(", ", missingFunctions)));
			}

			this.functions = functions;
			this.serializers = serializers;
			this.messageHandlers = new Dictionary<int, ClientMessageHandler>();
			this.logger = logger;
			foreach (var handler in messageHandlers)
			{
				this.messageHandlers[handler.Id] = handler;
			}
			pendingMessages = new Queue<Message>();
		}

		public void Connect(string ipAddress, ushort port) 
		{
			socket = NetworkConnectionUtils.CreatePlatformSpecificSocket<ConveyorSerializer>();
			socket.Start(UdpEndPoint.Any);
			socket.UserToken = serializers;
			socket.Connect(new UdpEndPoint(UdpIPv4Address.Parse(ipAddress), port));
		}

		public void Disconnect()
		{
			QueueMessage(
				new Message(BuiltInClientToServerMessages.Disconnect, null));
			SendPendingMessages();
			socket.Close();
		}

		public void Poll()
		{
			UdpEvent evt;
			while (socket.Poll(out evt)) 
			{
				switch (evt.EventType) 
				{
					case UdpEventType.SocketStarted:
						logger(LogType.Info, "Socket started");
						break;
					case UdpEventType.SocketStartupFailed:
						logger(LogType.Error, "Socket failed to start");
						break;
					case UdpEventType.ConnectFailed:
						functions.FailedToConnectToServer(evt.EndPoint);
						break;
					case UdpEventType.ConnectRefused:
						functions.ConnectionRefusedByServer(evt.EndPoint);
						break;
					case UdpEventType.Connected:
						server = evt.Connection;
						functions.ConnectedToServer(server.RemoteEndPoint);
						break;
					case UdpEventType.Disconnected:
						functions.LostConnectionToServer(evt.Connection.RemoteEndPoint);
						break;
					case UdpEventType.ObjectSendFailed:
						logger(LogType.Info, "object send failed");
						break;
					case UdpEventType.ObjectRejected:
						logger(LogType.Info, "object was rejected: " + evt.Object);
						break;
					case UdpEventType.ObjectDelivered:
						logger(LogType.Info, "object delivered");
						break;
					case UdpEventType.ObjectLost:
						logger(LogType.Info, "object lost");
						break;
					case UdpEventType.ObjectReceived:
						var message = (DeserializedMessage)evt.Object;
						if (BuiltInServerToClientMessages.InvalidMessage == message.Id)
						{
							QueueMessage(
								functions.CreateUnknownMessageMessage(message.OriginalId));
						}
						else if (!message.Data.HasValue)
						{
							QueueMessage(
								functions.CreateMalformedMessageMessage(message.Id));
						}
						else 
						{
							if (messageHandlers.ContainsKey(message.Id))
							{
								var handler = messageHandlers[message.Id];
								if (handler.Active())
								{
									
									handler.Handler(
										new Message 
										{
											Id = message.Id,
											Data = message.Data.Value
										});
								}
							}
							else
							{
								functions.MessageHandlerIsNotRegistered(message.Id);
							}
						}

						break;
					case UdpEventType.ObjectSent:
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
				if (!serializers.ContainsKey(message.Id))
				{
					functions.MessageSerializerIsNotRegistered(message.Id);
				}

				server.Send(message);
			}
		}

		public void QueueMessage(Message message)
		{
			pendingMessages.Enqueue(message);
		}

		static string[] FindMissingFunctions(ClientFunctions functions) 
		{
			return new List<string>
			{
				functions.ConnectedToServer == null ? "ConnectedToServer" : null,
				functions.ConnectionRefusedByServer == null ? "ConnectionRefusedByServer" : null,
				functions.CreateMalformedMessageMessage == null ? "CreateMalformedMessageMessage" : null,
				functions.CreateUnknownMessageMessage == null ? "CreateUnknownMessageMessage" : null,
				functions.LostConnectionToServer == null ? "LostConnectionToServer" : null,
				functions.FailedToConnectToServer == null ? "FailedToConnectToServer" : null,
				functions.MessageHandlerIsNotRegistered == null ? "MessageHandlerIsNotRegistered" : null,
				functions.MessageSerializerIsNotRegistered == null ? "MessageSerializerIsNotRegistered" : null,
			}
			.Where(str => !string.IsNullOrEmpty(str))
			.ToArray();
		}
	}
}