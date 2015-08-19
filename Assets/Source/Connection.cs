using System;

namespace Conveyor
{
	public enum ConnectionStatus
	{
		Connected,
		Joined,
		Disconnected,
		ConnectionLost,
	}

	[Serializable]
	public class Connection
	{
		[ReadOnly] public readonly Guid Id;
		[ReadOnly] public ConnectionStatus Status;

		public Connection() : this (Guid.NewGuid(), ConnectionStatus.Connected) { }

		public Connection(Guid id, ConnectionStatus status)
		{
			Id = id;
			Status = status;
		}
	}
}