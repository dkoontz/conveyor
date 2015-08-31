using System;
using System.IO;
using System.Collections.Generic;
using Avro.IO;
using Conveyor;


namespace Conveyor
{
	public class MalformedMessageMessage : IConveyable
	{
		
		public readonly int MessageId;
		
		public MalformedMessageMessage(int MessageId)
		{
			this.MessageId = MessageId;
		}
		
		public MalformedMessageMessage WithMessageId(int newValue)
		{
			return new MalformedMessageMessage(newValue);
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
			MalformedMessageMessage obj = (MalformedMessageMessage)data;
		
			encoder.WriteInt(obj.MessageId);
		}
		
		public static MalformedMessageMessage Deserialize(BinaryDecoder decoder)
		{
		
			int field0 = decoder.ReadInt();
		
			return new MalformedMessageMessage(
				field0);
		}
	}
}
