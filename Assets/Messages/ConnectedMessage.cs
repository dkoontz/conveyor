using System;

namespace Conveyor
{
	[Conveyable]
	public partial class ConnectedMessage : IConveyable
	{
		public Guid Id { get; private set; }
	}
}