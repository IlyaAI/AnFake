using System;
using AnFake.Api;
using Common.Logging;

namespace AnFake.Logging
{
	public abstract class LoggerFactoryAdapter : ILoggerFactoryAdapter, ILogConfig
	{
		protected static readonly string[] PrivilegedLoggers = { "AnFake.Trace", "AnFake.Trace" };

		public static ILogConfig CurrentConfig { get; private set; }

		protected LoggerFactoryAdapter()
		{
			CurrentConfig = this;
		}

		public ILog GetLogger(Type type)
		{
			return GetLogger(type.FullName);
		}

		public abstract ILog GetLogger(string name);

		public abstract void SetUp(string logPath, Verbosity verbosity);
	}
}