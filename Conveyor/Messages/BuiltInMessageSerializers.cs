using UnityEngine;
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
				return new Dictionary<int, MessageSerializers>() 
				{
					// TODO: Put back
//					{
//						(ushort)BuiltInServerToClientMessages.UnknownMessage,
//						new MessageSerializers(
//							BuiltInMessageSerializers.UnknownMessageMessageSerializer,
//							BuiltInMessageSerializers.UnknownMessageMessageDeserializer)
//					},
//					{
//						(ushort)BuiltInServerToClientMessages.MalformedMessage,
//						new MessageSerializers(
//							BuiltInMessageSerializers.MalformedMessageMessageSerializer,
//							BuiltInMessageSerializers.MalformedMessageMessageDeserializer)
//					},
//					{ 
//						(ushort)BuiltInServerToClientMessages.ServerConfiguration, 
//						new MessageSerializers(
//							BuiltInMessageSerializers.ServerConfigurationMessageSerializer,
//							BuiltInMessageSerializers.ServerConfigurationMessageDeserializer)
//					},
//					{
//						(ushort)BuiltInServerToClientMessages.ServerFull,
//						MessageSerializers.EmptySerializers
//					},
//					{
//						(ushort)BuiltInServerToClientMessages.AuthenticationRequired,
//						MessageSerializers.EmptySerializers
//					},
//					{
//						(ushort)BuiltInServerToClientMessages.InvalidAuthentication,
//						MessageSerializers.EmptySerializers
//					},
//					{
//						(ushort)BuiltInServerToClientMessages.Connected,
//						new MessageSerializers(
//							BuiltInMessageSerializers.ConnectedMessageSerializer,
//							BuiltInMessageSerializers.ConnectedMessageDeserializer)
//					},
//					{
//						(ushort)BuiltInServerToClientMessages.JoinedServer,
//						new MessageSerializers(
//							BuiltInMessageSerializers.JoinedServerMessageSerializer,
//							BuiltInMessageSerializers.JoinedServerMessageDeserializer)
//					},
//					{
//						(ushort)BuiltInClientToServerMessages.GetServerConfiguration,
//						MessageSerializers.EmptySerializers
//					},
//					{
//						(ushort)BuiltInClientToServerMessages.Login,
//						new MessageSerializers(
//							BuiltInMessageSerializers.LoginMessageSerializer,
//							BuiltInMessageSerializers.LoginMessageDeserializer)
//					},
//					{
//						(ushort)BuiltInClientToServerMessages.Reconnect,
//						new MessageSerializers(
//							BuiltInMessageSerializers.ReconnectMessageSerializer,
//							BuiltInMessageSerializers.ReconnectMessageDeserializer)
//					},
				};
			}
		}

//		public static bool ServerConfigurationMessageSerializer(IMessageData data, UdpStream stream) 
//		{
//			var config = (ServerConfigurationMessage)data;
//			stream.WriteBool(config.AuthenticationRequired);
//			stream.WriteByte((byte)config.MaxNumberOfPlayers);
//			stream.WriteByte((byte)config.CurrentNumberOfPlayers);
//			stream.WriteString(config.CustomData);
//
//			return true;
//		}
//
//		public static Maybe<IMessageData> ServerConfigurationMessageDeserializer(UdpStream stream) 
//		{
//			return new Maybe<IMessageData>(
//				new ServerConfigurationMessage(
//					stream.ReadBool(),
//					(int)stream.ReadByte(),
//					(int)stream.ReadByte(),
//					stream.ReadString()));
//		}
//
//		public static bool MalformedMessageMessageSerializer(IMessageData data, UdpStream stream)
//		{
//			var message = (MalformedMessageMessage)data;
//			stream.WriteUShort(message.MessageId);
//			return true;
//		}
//
//		public static Maybe<IMessageData> MalformedMessageMessageDeserializer(UdpStream stream) 
//		{
//			return new Maybe<IMessageData>(
//				new MalformedMessageMessage(stream.ReadUShort()));
//		}
//
//		public static bool UnknownMessageMessageSerializer(IMessageData data, UdpStream stream)
//		{
//			var message = (UnknownMessageMessage)data;
//			stream.WriteUShort(message.MessageId);
//			return true;
//		}
//
//		public static Maybe<IMessageData> UnknownMessageMessageDeserializer(UdpStream stream) 
//		{
//			return new Maybe<IMessageData>(
//				new UnknownMessageMessage(stream.ReadUShort()));
//		}
//
//		static bool ConnectedMessageSerializer(IMessageData data, UdpStream stream)
//		{
//			var message = (ConnectedMessage)data;
//			stream.WriteString(message.Id.ToString());
//			return true;
//		}
//
//		public static Maybe<IMessageData> ConnectedMessageDeserializer(UdpStream stream) 
//		{
//			var guidString = stream.ReadString();
//			try 
//			{
//				var guid = new Guid(guidString);
//				return new Maybe<IMessageData>(
//					new ConnectedMessage(guid));
//			}
//			catch
//			{
//				return Maybe<IMessageData>.Empty;
//			}
//		}
//
//		public static bool LoginMessageSerializer(IMessageData data, UdpStream stream)
//		{
//			var message = (LoginMessage)data;
//			stream.WriteString(message.UserId);
//			stream.WriteString(message.Authentication);
//			return true;
//		}
//
//		public static Maybe<IMessageData> LoginMessageDeserializer(UdpStream stream) 
//		{
//			return new Maybe<IMessageData>(
//				new LoginMessage(
//					stream.ReadString(),
//					stream.ReadString()));
//		}
//
//		public static bool JoinedServerMessageSerializer(IMessageData data, UdpStream stream)
//		{
//			var message = (JoinedServerMessage)data;
//			stream.WriteString(message.Data);
//			return true;
//		}
//
//		public static Maybe<IMessageData> JoinedServerMessageDeserializer(UdpStream stream) 
//		{
//			return new Maybe<IMessageData>(
//				new JoinedServerMessage(
//					stream.ReadString()));
//		}
//
//		public static bool ReconnectMessageSerializer(IMessageData data, UdpStream stream)
//		{
//			var message = (ReconnectMessage)data;
//			stream.WriteString(message.OriginalId.ToString());
//			return true;
//		}
//
//		public static Maybe<IMessageData> ReconnectMessageDeserializer(UdpStream stream) 
//		{
//			var guidString = stream.ReadString();
//			try 
//			{
//				var guid = new Guid(guidString);
//				return new Maybe<IMessageData>(
//					new ReconnectMessage(guid));
//			}
//			catch
//			{
//				return Maybe<IMessageData>.Empty;
//			}
//		}
	}
}

