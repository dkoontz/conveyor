using System;

namespace Conveyor
{
	[Conveyable]
	public partial class ServerConfigurationMessage : IConveyable 
	{
		public bool AuthenticationRequired { get; private set; }
		public int MaxNumberOfPlayers { get; private set; }
		public int CurrentNumberOfPlayers { get; private set; }
		public string CustomData { get; private set; }
	}
}