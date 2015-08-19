using System;
using System.IO;
using System.Collections.Generic;
using Avro.IO;

namespace Conveyor
{
	public partial class LoginMessage : IConveyable
	{
		
		public LoginMessage(System.String UserId, System.String Authentication)
		{
			this.UserId = UserId;
			this.Authentication = Authentication;
		}
		
		public LoginMessage WithUserId(System.String newValue)
		{
			return new LoginMessage(newValue, Authentication);
		}
		
		public LoginMessage WithAuthentication(System.String newValue)
		{
			return new LoginMessage(UserId, newValue);
		}
		
		public static void Serialize(IConveyable data, BinaryEncoder encoder)
		{
			LoginMessage obj = (LoginMessage)data;
		
			encoder.WriteString(obj.UserId);
		
			encoder.WriteString(obj.Authentication);
		}
		
		public static LoginMessage Deserialize(BinaryDecoder decoder)
		{
		
			System.String property0 = decoder.ReadString();
		
			System.String property1 = decoder.ReadString();
		
			return new LoginMessage(
				property0,
				property1);
		}
	}
}
