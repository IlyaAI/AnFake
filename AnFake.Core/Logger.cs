using Common.Logging;

namespace AnFake.Core
{
	public static class Logger
	{
		private static readonly ILog Log = LogManager.GetLogger("AnFake.Build");

		public static void Debug(object message)
		{
			Log.Debug(message);
		}

		public static void DebugFormat(string format, params object[] args)
		{
			Log.DebugFormat(format, args);
		}

		public static void Info(object message)
		{
			Log.Info(message);
		}

		public static void InfoFormat(string format, params object[] args)
		{
			Log.InfoFormat(format, args);
		}

		public static void Warn(object message)
		{
			Log.Warn(message);
		}

		public static void WarnFormat(string format, params object[] args)
		{
			Log.WarnFormat(format, args);
		}

		public static void Error(object message)
		{
			Log.Error(message);
		}

		public static void ErrorFormat(string format, params object[] args)
		{
			Log.ErrorFormat(format, args);
		}
	}
}