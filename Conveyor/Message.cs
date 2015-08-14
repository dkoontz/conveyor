using System;
using Avro.IO;

namespace Conveyor 
{
	public struct Message 
	{
		public int Id;
		public IMessageData Data;

		public Message(int id, IMessageData data)
		{
			Id = id;
			Data = data;
		}
	}
		
	public class MessageSerializers
	{
		public readonly Action<object, BinaryEncoder> Serializer;
		public readonly Func<BinaryDecoder, object> Deserializer;

		public MessageSerializers(Action<object, BinaryEncoder> serializer, Func<BinaryDecoder, object> deserializer)
		{
			Serializer = serializer;
			Deserializer = deserializer;
		}
	}
}