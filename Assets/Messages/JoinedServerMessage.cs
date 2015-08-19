using System;

namespace Conveyor
{
	[Conveyable]
	public partial class JoinedServerMessage : IConveyable
	{
		public string Data { get; private set; }
	}
}