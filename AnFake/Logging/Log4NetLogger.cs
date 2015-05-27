using System.Text;
using AnFake.Api;
using log4net;
using log4net.Appender;
using log4net.Core;
using log4net.Repository.Hierarchy;

namespace AnFake.Logging
{
	internal class Log4NetLogger : Api.ILogger
	{
		private const string LoggerName = "AnFake";

		private readonly log4net.Core.ILogger _logger;

		public Log4NetLogger(string logPath, Verbosity verbosity, int consoleWidth)
		{
			SetUp(logPath, verbosity, consoleWidth);

			_logger = LogManager.GetLogger(LoggerName).Logger;

			Threshold = verbosity.AsLogLevelThreshold();
		}

		public LogMessageLevel Threshold { get; set; }

		public void Write(LogMessageLevel level, string message)
		{
			if (level < Threshold)
				return;

			_logger.Log(typeof(Log4NetLogger), GetLevel(level), message, null);
		}

		private void SetUp(string logPath, Verbosity verbosity, int consoleWidth)
		{
			IAppender consoleAppender;
			if (Runtime.IsMono)
			{
				var monoConsoleAppender = new ConsoleAppender
				{
					Threshold = Level.All,
					Layout = new ConsolePattern(consoleWidth)
				};
				monoConsoleAppender.ActivateOptions();

				consoleAppender = monoConsoleAppender;
			}
			else
			{
				var coloredConsoleAppender = new ColoredConsoleAppender
				{
					Threshold = Level.All,
					Layout = new ConsolePattern(consoleWidth)
				};
				coloredConsoleAppender.AddMapping(
					new ColoredConsoleAppender.LevelColors
					{
						ForeColor = ColoredConsoleAppender.Colors.White,
						Level = Level.Trace
					});
				coloredConsoleAppender.AddMapping(
					new ColoredConsoleAppender.LevelColors
					{
						ForeColor = ColoredConsoleAppender.Colors.White | ColoredConsoleAppender.Colors.HighIntensity,
						Level = Level.Debug
					});
				coloredConsoleAppender.AddMapping(
					new ColoredConsoleAppender.LevelColors
					{
						ForeColor = ColoredConsoleAppender.Colors.Green | ColoredConsoleAppender.Colors.HighIntensity,
						Level = Level.Info
					});
				coloredConsoleAppender.AddMapping(
					new ColoredConsoleAppender.LevelColors
					{
						ForeColor = ColoredConsoleAppender.Colors.Yellow | ColoredConsoleAppender.Colors.HighIntensity,
						Level = Level.Warn
					});
				coloredConsoleAppender.AddMapping(
					new ColoredConsoleAppender.LevelColors
					{
						ForeColor = ColoredConsoleAppender.Colors.Red | ColoredConsoleAppender.Colors.HighIntensity,
						Level = Level.Error
					});
				coloredConsoleAppender.ActivateOptions();

				consoleAppender = coloredConsoleAppender;
			}			

			var fileAppender = new FileAppender
			{
				Threshold = Level.All,
				AppendToFile = false,
				Encoding = Encoding.UTF8,
				File = logPath,
				Layout = new FilePattern(LoggerName)
			};
			fileAppender.ActivateOptions();

			var hierarchy = (Hierarchy)LogManager.GetRepository();

			var root = hierarchy.Root;
			root.AddAppender(fileAppender);
			root.Level = GetLevelThreshold(verbosity);

			var anfake = hierarchy.GetLogger(LoggerName, hierarchy.LoggerFactory);
			anfake.AddAppender(consoleAppender);
			anfake.Level = Level.All;

			hierarchy.Configured = true;
		}

		private static Level GetLevel(LogMessageLevel msgLevel)
		{
			switch (msgLevel)
			{
				case LogMessageLevel.Debug:
				case LogMessageLevel.Details:
					return Level.Trace;
				case LogMessageLevel.Info:
				case LogMessageLevel.Summary:
				case LogMessageLevel.Text:
					return Level.Debug;
				case LogMessageLevel.Success:
					return Level.Info;
				case LogMessageLevel.Warning:
					return Level.Warn;
				case LogMessageLevel.Error:
					return Level.Error;
				default:
					return Level.Off;
			}
		}

		private static Level GetLevelThreshold(Verbosity verbosity)
		{
			switch (verbosity)
			{
				case Verbosity.Quiet:
				case Verbosity.Minimal:
					return Level.Warn;
					
				case Verbosity.Detailed:
					return Level.Debug;

				case Verbosity.Diagnostic:
					return Level.All;
					
				default:
					return Level.Info;
			}
		}
	}
}