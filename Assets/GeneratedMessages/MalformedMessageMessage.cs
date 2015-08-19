using System;
using System.IO;
using System.Collections.Generic;
using Avro.IO;

namespace Conveyor
{
	public partial class MalformedMessageMessage : IConveyable
	{
		
		public MalformedMessageMessage(System.Int32 MessageId)
		{
			this.MessageId = MessageId;
		}
		
		public MalformedMessageMessage WithMessageId(System.Int32 newValue)
		{
			return new MalformedMessageMessage(newValue);
		}
		
		public static void Serialize(IConveyable data, BinaryEncoder encoder)
		{
			MalformedMessageMessage obj = (MalformedMessageMessage)data;
		
			encoder.WriteInt(obj.MessageId);
		}
		
		public static MalformedMessageMessage Deserialize(BinaryDecoder decoder)
		{
		
			System.Int32 property0 = decoder.ReadInt();
		
			return new MalformedMessageMessage(
				property0);
		}
	}
}
