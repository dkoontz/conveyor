using System;
using System.Collections;
using System.Collections.Generic;
using UdpKit;

namespace Conveyor
{
	public static class BuiltInMessageSerializers 
	{
		public static Dictionary<int, MessageSerializers> DefaultSerializers 
		{
			get
			{
				return new Dictionary<int, MessageSerializers>
				{
					{
						ServerToClientMessages.UnknownMessage,
						new MessageSerializers(UnknownMessageMessage.Serialize, UnknownMessageMessage.Deserialize)
					},
					{
						(ushort)ServerToClientMessages.MalformedMessage,
						new MessageSerializers(MalformedMessageMessage.Serialize, MalformedMessageMessage.Deserialize)
					},
					{ 
						(ushort)ServerToClientMessages.ServerConfiguration, 
						new MessageSerializers(ServerConfigurationMessage.Serialize, ServerConfigurationMessage.Deserialize)
					},
					{
						(ushort)ServerToClientMessages.ServerFull,
						EmptyMessageSerializer.Serializers
					},
					{
						(ushort)ServerToClientMessages.AuthenticationRequired,
						EmptyMessageSerializer.Serializers
					},
					{
						(ushort)ServerToClientMessages.InvalidAuthentication,
						EmptyMessageSerializer.Serializers
					},
					{
						(ushort)ServerToClientMessages.Connected,
						new MessageSerializers(ConnectedMessage.Serialize, ConnectedMessage.Deserialize)
					},
					{
						(ushort)ServerToClientMessages.JoinedServer,
						new MessageSerializers(JoinedServerMessage.Serialize, JoinedServerMessage.Deserialize)
					},
					{
						(ushort)ClientToServerMessages.GetServerConfiguration,
						EmptyMessageSerializer.Serializers
					},
					{
						(ushort)ClientToServerMessages.Login,
						new MessageSerializers(LoginMessage.Serialize, LoginMessage.Deserialize)
					},
					{
						(ushort)ClientToServerMessages.Reconnect,
						new MessageSerializers(ReconnectMessage.Serialize, ReconnectMessage.Deserialize)
					},
				};
			}
		}
	}
}