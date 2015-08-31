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
						BuiltInServerToClientMessages.UnknownMessage,
						UnknownMessageMessage.Serializers
					},
					{
						(ushort)BuiltInServerToClientMessages.MalformedMessage,
						MalformedMessageMessage.Serializers
					},
					{ 
						(ushort)BuiltInServerToClientMessages.ServerConfiguration, 
						ServerConfigurationMessage.Serializers
					},
					{
						(ushort)BuiltInServerToClientMessages.ServerFull,
						EmptyMessageSerializer.Serializers
					},
					{
						(ushort)BuiltInServerToClientMessages.AuthenticationRequired,
						EmptyMessageSerializer.Serializers
					},
					{
						(ushort)BuiltInServerToClientMessages.InvalidAuthentication,
						EmptyMessageSerializer.Serializers
					},
					{
						(ushort)BuiltInServerToClientMessages.Connected,
						ConnectedMessage.Serializers
					},
					{
						(ushort)BuiltInServerToClientMessages.JoinedServer,
						JoinedServerMessage.Serializers
					},
					{
						(ushort)BuiltInClientToServerMessages.GetServerConfiguration,
						EmptyMessageSerializer.Serializers
					},
					{
						(ushort)BuiltInClientToServerMessages.Login,
						LoginMessage.Serializers
					},
					{
						(ushort)BuiltInClientToServerMessages.Reconnect,
						ReconnectMessage.Serializers
					},
					{
						(ushort)BuiltInClientToServerMessages.Disconnect,
						EmptyMessageSerializer.Serializers
					},
				};
			}
		}
	}
}