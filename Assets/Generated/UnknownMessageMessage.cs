using System;
using System.IO;
using System.Collections.Generic;
using Avro.IO;
using Conveyor;


namespace Conveyor
{
	public class UnknownMessageMessage : IConveyable
	{
		
		public readonly int MessageId;
		
		public UnknownMessageMessage(int MessageId)
		{
			this.MessageId = MessageId;
		}
		
		public UnknownMessageMessage WithMessageId(int newValue)
		{
			return new UnknownMessageMessage(newValue);
		}
		
		public static MessageSerializers Serializers
		{
			get
			{
				return new MessageSerializers(
					Serialize,
					Deserialize);
			}
		}
		
		public static void Serialize(IConveyable data, BinaryEncoder encoder)
		{
			UnknownMessageMessage obj = (UnknownMessageMessage)data;
		
			encoder.WriteInt(obj.MessageId);
		}
		
		public static UnknownMessageMessage Deserialize(BinaryDecoder decoder)
		{
		
			int field0 = decoder.ReadInt();
		
			return new UnknownMessageMessage(
				field0);
		}
	}
}
