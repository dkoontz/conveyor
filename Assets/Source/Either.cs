using System;

namespace Conveyor
{
	public class Either<T1, T2>
	{
		T1 left;
		T2 right;
		bool leftSet;

		public bool IsLeft
		{
			get 
			{
				return leftSet;
			}
		}

		public bool IsRight
		{
			get
			{
				return !leftSet;
			}
		}

		public T1 LeftValue
		{
			get 
			{
				return left;
			}
		}

		public T2 RightValue
		{
			get 
			{
				return right;
			}
		}

		public static Either<T1, T2> Left(T1 leftValue)
		{
			return new Either<T1, T2>
			{
				left = leftValue,
				leftSet = true
			};
		}

		public static Either<T1, T2> Right(T2 rightValue)
		{
			return new Either<T1, T2>
			{
				right = rightValue
			};
		}

		private Either() { }
	}
}