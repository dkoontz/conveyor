using System;
using System.IO;
using System.Collections.Generic;
using Avro.IO;
using Conveyor;


namespace Conveyor
{
	public class ServerConfigurationMessage : IConveyable
	{
		
		public readonly bool AuthenticationRequired;
		public readonly int MaxNumberOfPlayers;
		public readonly int CurrentNumberOfPlayers;
		public readonly string CustomData;
		
		public ServerConfigurationMessage(bool AuthenticationRequired, int MaxNumberOfPlayers, int CurrentNumberOfPlayers, string CustomData)
		{
			this.AuthenticationRequired = AuthenticationRequired;
			this.MaxNumberOfPlayers = MaxNumberOfPlayers;
			this.CurrentNumberOfPlayers = CurrentNumberOfPlayers;
			this.CustomData = CustomData;
		}
		
		public ServerConfigurationMessage WithAuthenticationRequired(bool newValue)
		{
			return new ServerConfigurationMessage(newValue, MaxNumberOfPlayers, CurrentNumberOfPlayers, CustomData);
		}
		
		public ServerConfigurationMessage WithMaxNumberOfPlayers(int newValue)
		{
			return new ServerConfigurationMessage(AuthenticationRequired, newValue, CurrentNumberOfPlayers, CustomData);
		}
		
		public ServerConfigurationMessage WithCurrentNumberOfPlayers(int newValue)
		{
			return new ServerConfigurationMessage(AuthenticationRequired, MaxNumberOfPlayers, newValue, CustomData);
		}
		
		public ServerConfigurationMessage WithCustomData(string newValue)
		{
			return new ServerConfigurationMessage(AuthenticationRequired, MaxNumberOfPlayers, CurrentNumberOfPlayers, newValue);
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
			ServerConfigurationMessage obj = (ServerConfigurationMessage)data;
		
			encoder.WriteBoolean(obj.AuthenticationRequired);
		
			encoder.WriteInt(obj.MaxNumberOfPlayers);
		
			encoder.WriteInt(obj.CurrentNumberOfPlayers);
		
			encoder.WriteString(obj.CustomData);
		}
		
		public static ServerConfigurationMessage Deserialize(BinaryDecoder decoder)
		{
		
			bool field0 = decoder.ReadBoolean();
		
			int field1 = decoder.ReadInt();
		
			int field2 = decoder.ReadInt();
		
			string field3 = decoder.ReadString();
		
			return new ServerConfigurationMessage(
				field0,
				field1,
				field2,
				field3);
		}
	}
}
