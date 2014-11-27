using System;
using Common.Logging;

namespace AnFake.Core
{
	public static class SafeOp
	{
		private static readonly ILog Log = LogManager.GetLogger(typeof (SafeOp).FullName);

		public static bool Try<T1>(Action<T1> action, T1 arg1)
		{
			try
			{
				action(arg1);

				return true;
			}
			catch (Exception e)
			{
				Log.Error(e);
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
				Log.Error(e);
			}

			return false;
		}
	}
}