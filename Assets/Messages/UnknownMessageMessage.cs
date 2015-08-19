using System;

namespace Conveyor
{
	[Conveyable]
	public partial class UnknownMessageMessage : IConveyable
	{
		public int MessageId { get; private set; }
	}
}