using System;
using AnFake.Api;

namespace AnFake.Core
{
	public static class SafeOp
	{
		public static bool Try<T1>(Action<T1> action, T1 arg1)
		{
			try
			{
				action(arg1);

				return true;
			}
			catch (Exception e)
			{
				Log.Error("SafeOp.Try: {0}", e);
			}

			return false;
		}

		public static bool Try<T1, T2>(Action<T1, T2> action, T1 arg1, T2 arg2)
		{
			try
			{
				action(arg1, arg2);

				return true;
			}
			catch (Exception e)
			{
				Log.Error("SafeOp.Try: {0}", e);
			}

			return false;
		}

		public static bool Try<T1, T2, T3>(Action<T1, T2, T3> action, T1 arg1, T2 arg2, T3 arg3)
		{
			try
			{
				action(arg1, arg2, arg3);

				return true;
			}
			catch (Exception e)
			{
				Log.Error("SafeOp.Try: {0}", e);
			}

			return false;
		}
	}
}