using System;
using UdpKit;
using System.Collections.Generic;
using System.IO;
using Avro.IO;

namespace Conveyor 
{
	class ConveyorSerializer : UdpSerializer 
	{
		public override bool Pack (UdpStream stream, ref object message) 
		{
			var msg = (Message)message;
			var messageRegistrations = (Dictionary<int, MessageSerializers>)Connection.Socket.UserToken;

			if (messageRegistrations.ContainsKey(msg.Id))
			{
				stream.WriteInt(msg.Id);
				var encodedStream = new MemoryStream();
				var encoder = new BinaryEncoder(encodedStream);
				messageRegistrations[msg.Id].Serializer(msg.Data, encoder);

				var data = encodedStream.ToArray();
				stream.WriteUInt((uint)data.Length);
				stream.WriteByteArray(data);
				return true;
			}

			return false;
		}

		public override bool Unpack (UdpStream stream, ref object deserializedMessage) 
		{
			var messageRegistrations = (Dictionary<int, MessageSerializers>)Connection.Socket.UserToken;
			int id = stream.ReadInt();
			if (messageRegistrations.ContainsKey(id)) 
			{
				var length = stream.ReadUInt ();
				var bytes = new byte[length];
				stream.ReadByteArray(bytes);
				var decoder = new BinaryDecoder(new MemoryStream(bytes));
				try
				{
					var data = messageRegistrations[id].Deserializer(decoder);
					deserializedMessage = new DeserializedMessage
					{
						Id = id,
						Data = new Maybe<IMessageData>(data as IMessageData)
					};
				}
				catch
				{
					deserializedMessage = new DeserializedMessage
					{
						Id = id,
						Data = Maybe<IMessageData>.Empty
					};
				}
			}
			else
			{
				deserializedMessage = new DeserializedMessage
				{
					Id = ServerToClientMessages.InvalidMessage,
					OriginalId = id,
					Data = null
				};
			}

			return true;
		}

		// Utility serializers and deserializers ============================================

//		static void SerializePlayerControlsMessage(UdpStream stream, PlayerControlsMessage message) {
//			SerializeVector2H(stream, message.LeftStick);
//			SerializeVector2H(stream, message.RightStick);
//			SerializeEnumArray(stream, message.Events);
//		}
//
//		static PlayerControlsMessage DeserializePlayerControlsMessage(UdpStream stream) {
//			return new PlayerControlsMessage {
//				LeftStick = DeserializeVector2H(stream),
//				RightStick = DeserializeVector2H(stream),
//				Events = DeserializeEnumArray<PlayerControlsMessage.PlayerControlEvent>(stream)				
//			};
//		}

//		static void SerializeVector2(UdpStream stream, Vector2 vector) {
//			stream.WriteFloat(vector.x);
//			stream.WriteFloat(vector.y);
//		}
//
//		static void SerializeVector2H(UdpStream stream, Vector2 vector) {
//			stream.WriteHalf(vector.x);
//			stream.WriteHalf(vector.y);
//		}
//
//		static Vector2 DeserializeVector2(UdpStream stream) {
//			var x = stream.ReadFloat();
//			var y = stream.ReadFloat();
//			return new Vector2(x, y);
//		}
//
//		static Vector2 DeserializeVector2H(UdpStream stream) {
//			var x = stream.ReadHalf();
//			var y = stream.ReadHalf();
//			return new Vector2(x, y);
//		}
//
//		static void SerializeEnumArray<T>(UdpStream stream, T[] array) where T : struct {
//			stream.WriteShort((short)array.Length);
//			foreach (var element in array) {
//				stream.WriteEnum32<T>(element);
//			}
//		}
//
//		static T[] DeserializeEnumArray<T>(UdpStream stream) where T : struct {
//			var elementCount = (int)stream.ReadShort();
//			var array = new T[elementCount];
//			for (var i = 0; i < elementCount; ++i) {
//				array[i] = stream.ReadEnum32<T>();
//			}
//			return array;
//		}
	}
}