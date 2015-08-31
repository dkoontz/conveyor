using System;
using System.IO;
using System.Collections.Generic;
using Avro.IO;
using Conveyor;


namespace Conveyor
{
	public class JoinedServerMessage : IConveyable
	{
		
		public readonly string Data;
		
		public JoinedServerMessage(string Data)
		{
			this.Data = Data;
		}
		
		public JoinedServerMessage WithData(string newValue)
		{
			return new JoinedServerMessage(newValue);
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
			JoinedServerMessage obj = (JoinedServerMessage)data;
		
			encoder.WriteString(obj.Data);
		}
		
		public static JoinedServerMessage Deserialize(BinaryDecoder decoder)
		{
		
			string field0 = decoder.ReadString();
		
			return new JoinedServerMessage(
				field0);
		}
	}
}
