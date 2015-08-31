using System;
using System.IO;
using System.Collections.Generic;
using Avro.IO;
using Conveyor;


namespace Conveyor
{
	public class ReconnectMessage : IConveyable
	{
		
		public readonly Guid OriginalId;
		
		public ReconnectMessage(Guid OriginalId)
		{
			this.OriginalId = OriginalId;
		}
		
		public ReconnectMessage WithOriginalId(Guid newValue)
		{
			return new ReconnectMessage(newValue);
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
			ReconnectMessage obj = (ReconnectMessage)data;
		
			GuidSerializer.Serializer(obj.OriginalId, encoder);
		}
		
		public static ReconnectMessage Deserialize(BinaryDecoder decoder)
		{
		
			Guid field0 = GuidSerializer.Deserializer(decoder);
		
			return new ReconnectMessage(
				field0);
		}
	}
}
