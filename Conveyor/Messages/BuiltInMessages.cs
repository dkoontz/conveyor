using System;

namespace Conveyor
{
	public interface IMessageData { }

	[Conveyable]
	public partial class MalformedMessageMessage : IMessageData
	{
		public Enum MessageId;
	}

	[Conveyable]
	public partial class UnknownMessageMessage : IMessageData
	{
		public Enum MessageId;
	}

	[Conveyable]
	public partial class ServerConfigurationMessage : IMessageData 
	{
		public bool AuthenticationRequired;
		public int MaxNumberOfPlayers;
		public int CurrentNumberOfPlayers;
		public string CustomData;
	}

	[Conveyable]
	public partial class LoginMessage : IMessageData
	{
		public string UserId;
		public string Authentication;
	}

	[Conveyable]
	public partial class JoinedServerMessage : IMessageData
	{
		public string Data;
	}

	[Conveyable]
	public partial class ConnectedMessage : IMessageData
	{
		public Guid Id;
	}

	[Conveyable]
	public partial class ReconnectMessage : IMessageData
	{
		public Guid OriginalId;
	}
}