using System;
using Avro.IO;

namespace Conveyor 
{
	public struct Message 
	{
		public int Id;
		public IConveyable Data;

		public Message(int id, IConveyable data)
		{
			Id = id;
			Data = data;
		}
	}
		
	public class MessageSerializers
	{
		public readonly Action<IConveyable, BinaryEncoder> Serializer;
		public readonly Func<BinaryDecoder, IConveyable> Deserializer;

		public MessageSerializers(Action<IConveyable, BinaryEncoder> serializer, Func<BinaryDecoder, IConveyable> deserializer)
		{
			Serializer = serializer;
			Deserializer = deserializer;
		}
	}

	public static class EmptyMessageSerializer
	{
		public static MessageSerializers Serializers
		{
			get
			{
				return new MessageSerializers(Serialize, Deserialize);
			}
		}

		public static void Serialize(IConveyable o, BinaryEncoder encoder) { }
		public static IConveyable Deserialize(BinaryDecoder decoder) { return null; }
	}
}