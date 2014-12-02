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
		private readonly log4net.Core.ILogger _logger;

		public Log4NetLogger(string logPath, int consoleWidth)
		{
			_logger = LogManager.GetLogger("AnFake").Logger;

			SetUp(logPath, consoleWidth);
		}

		public LogMessageLevel Threshold { get; set; }

		public void Write(LogMessageLevel level, string message)
		{
			if (level < Threshold)
				return;

			_logger.Log(typeof(Log4NetLogger), GetLevel(level), message, null);
		}

		private void SetUp(string logPath, int consoleWidth)
		{
			var consoleAppender = new ColoredConsoleAppender
			{
				Threshold = Level.All,
				Layout = new ConsolePattern(consoleWidth)
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
				Layout = new FilePattern()
			};

			var hierarchy = (Hierarchy)LogManager.GetRepository();

			consoleAppender.ActivateOptions();
			hierarchy.Root.AddAppender(consoleAppender);

			fileAppender.ActivateOptions();
			hierarchy.Root.AddAppender(fileAppender);

			hierarchy.Root.Level = Level.All;
			hierarchy.Configured = true;
		}

		private static Level GetLevel(LogMessageLevel msgLevel)
		{
			switch (msgLevel)
			{
				case LogMessageLevel.Debug:
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
	}
}