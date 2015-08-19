using System;
using System.IO;
using System.Collections.Generic;
using Avro.IO;

namespace Conveyor
{
	public partial class ConnectedMessage : IConveyable
	{
		
		public ConnectedMessage(System.Guid Id)
		{
			this.Id = Id;
		}
		
		public ConnectedMessage WithId(System.Guid newValue)
		{
			return new ConnectedMessage(newValue);
		}
		
		public static void Serialize(IConveyable data, BinaryEncoder encoder)
		{
			ConnectedMessage obj = (ConnectedMessage)data;
		
			GuidSerializer.Serializer(obj.Id, encoder);
		}
		
		public static ConnectedMessage Deserialize(BinaryDecoder decoder)
		{
		
			System.Guid property0 = GuidSerializer.Deserializer(decoder);
		
			return new ConnectedMessage(
				property0);
		}
	}
}
