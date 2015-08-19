using System;

namespace Conveyor
{
	[Conveyable]
	public partial class ReconnectMessage : IConveyable
	{
		public Guid OriginalId { get; private set; }
	}
}