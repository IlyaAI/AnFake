using System;
using System.Text;
using AnFake.Api;
using Common.Logging;
using Common.Logging.Factory;
using log4net.Appender;
using log4net.Core;
using log4net.Layout;
using log4net.Repository.Hierarchy;

namespace AnFake.Logging
{
	public sealed class Log4NetFactoryAdapter : LoggerFactoryAdapter
	{
		private class Log4NetLogger : AbstractLogger
		{
			private readonly ILogger _logger;

			public override bool IsTraceEnabled
			{
				get { return _logger.IsEnabledFor(Level.Trace); }
			}

			public override bool IsDebugEnabled
			{
				get { return _logger.IsEnabledFor(Level.Debug); }
			}

			public override bool IsInfoEnabled
			{
				get { return _logger.IsEnabledFor(Level.Info); }
			}

			public override bool IsWarnEnabled
			{
				get { return _logger.IsEnabledFor(Level.Warn); }
			}

			public override bool IsErrorEnabled
			{
				get { return _logger.IsEnabledFor(Level.Error); }
			}

			public override bool IsFatalEnabled
			{
				get { return _logger.IsEnabledFor(Level.Fatal); }
			}

			public Log4NetLogger(ILogger logger)
			{
				_logger = logger;
			}

			protected override void WriteInternal(LogLevel logLevel, object message, Exception exception)
			{
				_logger.Log(typeof (Log4NetLogger), GetLevel(logLevel), message, exception);
			}

			private static Level GetLevel(LogLevel logLevel)
			{
				switch (logLevel)
				{
					case LogLevel.All:
						return Level.All;
					case LogLevel.Trace:
						return Level.Trace;
					case LogLevel.Debug:
						return Level.Debug;
					case LogLevel.Info:
						return Level.Info;
					case LogLevel.Warn:
						return Level.Warn;
					case LogLevel.Error:
						return Level.Error;
					case LogLevel.Fatal:
						return Level.Fatal;
					default:
						return Level.Off;
				}
			}
		}

		public override ILog GetLogger(string name)
		{
			return new Log4NetLogger(log4net.LogManager.GetLogger(name).Logger);
		}

		public override void SetUp(string logPath, Verbosity verbosity)
		{
			var consoleAppender = new ColoredConsoleAppender
			{
				Threshold = Level.All,
				Layout = new PatternLayout("%message%newline")
			};
			consoleAppender.AddMapping(
				new ColoredConsoleAppender.LevelColors
				{
					ForeColor = ColoredConsoleAppender.Colors.White, 
					Level = Level.Trace
				});
			consoleAppender.AddMapping(
				new ColoredConsoleAppender.LevelColors
				{
					ForeColor = ColoredConsoleAppender.Colors.White | ColoredConsoleAppender.Colors.HighIntensity,
					Level = Level.Debug
				});
			consoleAppender.AddMapping(
				new ColoredConsoleAppender.LevelColors
				{
					ForeColor = ColoredConsoleAppender.Colors.Green | ColoredConsoleAppender.Colors.HighIntensity,
					Level = Level.Info
				});
			consoleAppender.AddMapping(
				new ColoredConsoleAppender.LevelColors
				{
					ForeColor = ColoredConsoleAppender.Colors.Yellow | ColoredConsoleAppender.Colors.HighIntensity,
					Level = Level.Warn
				});
			consoleAppender.AddMapping(
				new ColoredConsoleAppender.LevelColors
				{
					ForeColor = ColoredConsoleAppender.Colors.Red | ColoredConsoleAppender.Colors.HighIntensity,
					Level = Level.Error
				});

			var fileAppender = new FileAppender
			{
				Threshold = Level.All,
				AppendToFile = false,
				Encoding = Encoding.UTF8,
				File = logPath,
				Layout = new PatternLayout("[%-5level] %message%newline")
			};
			
			var hierarchy = (Hierarchy) log4net.LogManager.GetRepository();

			consoleAppender.ActivateOptions();
			hierarchy.Root.AddAppender(consoleAppender);

			fileAppender.ActivateOptions();
			hierarchy.Root.AddAppender(fileAppender);

			switch (verbosity)
			{
				case Verbosity.Quiet:
					hierarchy.Root.Level = Level.Warn;
					break;
				case Verbosity.Detailed:
				case Verbosity.Diagnostic:				
					hierarchy.Root.Level = Level.Trace;
					break;
				default:
					hierarchy.Root.Level = Level.Debug;
					break;
			}

			foreach (var name in PrivilegedLoggers)
			{
				hierarchy.GetLogger(name, hierarchy.LoggerFactory).Level =
					verbosity >= Verbosity.Detailed ? Level.Trace : Level.Debug;
			}

			hierarchy.Configured = true;
		}
	}
}