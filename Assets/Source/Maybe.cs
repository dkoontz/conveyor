using System;

namespace Conveyor {
	public class Maybe<T>
	{
		public readonly static Maybe<T> Empty = new Maybe<T>();

		public T Value { get; private set; }

		public bool HasValue { get; private set; }

		Maybe()
		{
			HasValue = false;
		}

		public Maybe(T value)
		{
			Value = value;
			HasValue = true;
		}
	}

	public static class MaybeExtensions {
		public static Maybe<T> ToMaybe<T>(this Nullable<T> obj) where T : struct
		{
			return obj.HasValue
				? new Maybe<T>(obj.Value)
				: Maybe<T>.Empty;
		}

		public static Maybe<T> ToMaybe<T>(this T value)
		{
			if (!(value is ValueType))
			{
				if (object.ReferenceEquals(value, null))
				{
					return Maybe<T>.Empty;
				}
			}

			return new Maybe<T>(value);
		}

		public static void Then<T>(this Maybe<T> m, Action<T> action)
		{
			if (m.HasValue)
			{
				action(m.Value);
			}
		}

		// Alias to Bind since monadic bind isn't very idiomatic in C#
		public static Maybe<T2> Select<T, T2>(this Maybe<T> m, Func<T, Maybe<T2>> transformer)
		{
			return Bind(m, transformer);
		}

		public static Maybe<T2> Bind<T, T2>(this Maybe<T> m, Func<T, Maybe<T2>> transformer)
		{
			return !m.HasValue
				? Maybe<T2>.Empty
				: transformer(m.Value);
		}
	}
}
