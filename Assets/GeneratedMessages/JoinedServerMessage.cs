using System;
using System.IO;
using System.Collections.Generic;
using Avro.IO;

namespace Conveyor
{
	public partial class JoinedServerMessage : IConveyable
	{
		
		public JoinedServerMessage(System.String Data)
		{
			this.Data = Data;
		}
		
		public JoinedServerMessage WithData(System.String newValue)
		{
			return new JoinedServerMessage(newValue);
		}
		
		public static void Serialize(IConveyable data, BinaryEncoder encoder)
		{
			JoinedServerMessage obj = (JoinedServerMessage)data;
		
			encoder.WriteString(obj.Data);
		}
		
		public static JoinedServerMessage Deserialize(BinaryDecoder decoder)
		{
		
			System.String property0 = decoder.ReadString();
		
			return new JoinedServerMessage(
				property0);
		}
	}
}
