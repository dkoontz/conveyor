using System;
using System.IO;
using System.Collections.Generic;
using Avro.IO;

namespace Conveyor
{
	public partial class ReconnectMessage : IConveyable
	{
		
		public ReconnectMessage(System.Guid OriginalId)
		{
			this.OriginalId = OriginalId;
		}
		
		public ReconnectMessage WithOriginalId(System.Guid newValue)
		{
			return new ReconnectMessage(newValue);
		}
		
		public static void Serialize(IConveyable data, BinaryEncoder encoder)
		{
			ReconnectMessage obj = (ReconnectMessage)data;
		
			GuidSerializer.Serializer(obj.OriginalId, encoder);
		}
		
		public static ReconnectMessage Deserialize(BinaryDecoder decoder)
		{
		
			System.Guid property0 = GuidSerializer.Deserializer(decoder);
		
			return new ReconnectMessage(
				property0);
		}
	}
}
