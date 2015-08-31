using System;
using System.IO;
using System.Collections.Generic;
using Avro.IO;
using Conveyor;


namespace Conveyor
{
	public class LoginMessage : IConveyable
	{
		
		public readonly string UserId;
		public readonly string Authentication;
		
		public LoginMessage(string UserId, string Authentication)
		{
			this.UserId = UserId;
			this.Authentication = Authentication;
		}
		
		public LoginMessage WithUserId(string newValue)
		{
			return new LoginMessage(newValue, Authentication);
		}
		
		public LoginMessage WithAuthentication(string newValue)
		{
			return new LoginMessage(UserId, newValue);
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
			LoginMessage obj = (LoginMessage)data;
		
			encoder.WriteString(obj.UserId);
		
			encoder.WriteString(obj.Authentication);
		}
		
		public static LoginMessage Deserialize(BinaryDecoder decoder)
		{
		
			string field0 = decoder.ReadString();
		
			string field1 = decoder.ReadString();
		
			return new LoginMessage(
				field0,
				field1);
		}
	}
}
