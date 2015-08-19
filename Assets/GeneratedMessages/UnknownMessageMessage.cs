using System;
using System.IO;
using System.Collections.Generic;
using Avro.IO;

namespace Conveyor
{
	public partial class UnknownMessageMessage : IConveyable
	{
		
		public UnknownMessageMessage(System.Int32 MessageId)
		{
			this.MessageId = MessageId;
		}
		
		public UnknownMessageMessage WithMessageId(System.Int32 newValue)
		{
			return new UnknownMessageMessage(newValue);
		}
		
		public static void Serialize(IConveyable data, BinaryEncoder encoder)
		{
			UnknownMessageMessage obj = (UnknownMessageMessage)data;
		
			encoder.WriteInt(obj.MessageId);
		}
		
		public static UnknownMessageMessage Deserialize(BinaryDecoder decoder)
		{
		
			System.Int32 property0 = decoder.ReadInt();
		
			return new UnknownMessageMessage(
				property0);
		}
	}
}
