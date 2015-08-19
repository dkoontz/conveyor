using System;

namespace Conveyor
{
	[Conveyable]
	public partial class LoginMessage
	{
		public string UserId { get; private set; }
		public string Authentication { get; private set; }
	}
}