using System;
using System.IO;
using System.Collections.Generic;
using Avro.IO;

namespace Conveyor
{
	public partial class ServerConfigurationMessage : IConveyable
	{
		
		public ServerConfigurationMessage(System.Boolean AuthenticationRequired, System.Int32 MaxNumberOfPlayers, System.Int32 CurrentNumberOfPlayers, System.String CustomData)
		{
			this.AuthenticationRequired = AuthenticationRequired;
			this.MaxNumberOfPlayers = MaxNumberOfPlayers;
			this.CurrentNumberOfPlayers = CurrentNumberOfPlayers;
			this.CustomData = CustomData;
		}
		
		public ServerConfigurationMessage WithAuthenticationRequired(System.Boolean newValue)
		{
			return new ServerConfigurationMessage(newValue, MaxNumberOfPlayers, CurrentNumberOfPlayers, CustomData);
		}
		
		public ServerConfigurationMessage WithMaxNumberOfPlayers(System.Int32 newValue)
		{
			return new ServerConfigurationMessage(AuthenticationRequired, newValue, CurrentNumberOfPlayers, CustomData);
		}
		
		public ServerConfigurationMessage WithCurrentNumberOfPlayers(System.Int32 newValue)
		{
			return new ServerConfigurationMessage(AuthenticationRequired, MaxNumberOfPlayers, newValue, CustomData);
		}
		
		public ServerConfigurationMessage WithCustomData(System.String newValue)
		{
			return new ServerConfigurationMessage(AuthenticationRequired, MaxNumberOfPlayers, CurrentNumberOfPlayers, newValue);
		}
		
		public static void Serialize(IConveyable data, BinaryEncoder encoder)
		{
			ServerConfigurationMessage obj = (ServerConfigurationMessage)data;
		
			encoder.WriteBoolean(obj.AuthenticationRequired);
		
			encoder.WriteInt(obj.MaxNumberOfPlayers);
		
			encoder.WriteInt(obj.CurrentNumberOfPlayers);
		
			encoder.WriteString(obj.CustomData);
		}
		
		public static ServerConfigurationMessage Deserialize(BinaryDecoder decoder)
		{
		
			System.Boolean property0 = decoder.ReadBoolean();
		
			System.Int32 property1 = decoder.ReadInt();
		
			System.Int32 property2 = decoder.ReadInt();
		
			System.String property3 = decoder.ReadString();
		
			return new ServerConfigurationMessage(
				property0,
				property1,
				property2,
				property3);
		}
	}
}
