using System;

namespace AnFake.Api
{
	public static class Interruption
	{
		public class BuildInterruptedException : Exception
		{
			internal BuildInterruptedException()
				: base("Build is interrupted.")
			{
			}
		}

		private static readonly object Mutex = new object();
		private static bool _interrupted;

		public static void CheckPoint()
		{
			lock (Mutex)
			{
				if (!_interrupted)
					return;
			}			

			Log.Debug("Interruption check-point activated.");
			
			throw new BuildInterruptedException();
		}

		public static void Requested()
		{
			Log.Debug("Interruption requested.");

			lock (Mutex)
			{
				_interrupted = true;
			}			
		}
	}
}