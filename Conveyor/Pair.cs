using System;

namespace Conveyor
{
	public struct Pair<T, U> {
		public readonly T First;
		public readonly U Second;

		public Pair(T first, U second) : this() {
			First = first;
			Second = second;
		}

		public override string ToString() {
			return string.Format("Pair({0}, {1})", First, Second);
		}
	}
}