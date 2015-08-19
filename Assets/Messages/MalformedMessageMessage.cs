using System;

namespace Conveyor
{
	[Conveyable]
	public partial class MalformedMessageMessage : IConveyable
	{
		public int MessageId { get; private set; }
	}
}