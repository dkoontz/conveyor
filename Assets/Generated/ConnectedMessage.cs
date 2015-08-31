using System;
using System.IO;
using System.Collections.Generic;
using Avro.IO;
using Conveyor;


namespace Conveyor
{
	public class ConnectedMessage : IConveyable
	{
		
		public readonly Guid Id;
		
		public ConnectedMessage(Guid Id)
		{
			this.Id = Id;
		}
		
		public ConnectedMessage WithId(Guid newValue)
		{
			return new ConnectedMessage(newValue);
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
			ConnectedMessage obj = (ConnectedMessage)data;
		
			GuidSerializer.Serializer(obj.Id, encoder);
		}
		
		public static ConnectedMessage Deserialize(BinaryDecoder decoder)
		{
		
			Guid field0 = GuidSerializer.Deserializer(decoder);
		
			return new ConnectedMessage(
				field0);
		}
	}
}
