using System.IO;
using AnFake.Core;
using log4net.Core;
using log4net.Layout;

namespace AnFake.Logging
{
	internal sealed class FilePattern : LayoutSkeleton
	{
		private readonly string _defaultLoggerName;

		public FilePattern(string defaultLoggerName)
		{
			_defaultLoggerName = defaultLoggerName;
		}

		public override void ActivateOptions()
		{
		}

		public override void Format(TextWriter writer, LoggingEvent loggingEvent)
		{
			var msg = loggingEvent.RenderedMessage;
			if (msg == null)
				return;

			var level = FormatLevel(loggingEvent.Level);

			if (loggingEvent.LoggerName == _defaultLoggerName)
			{				
				foreach (var line in msg.GetLines())
				{
					writer.Write(level);
					writer.Write(' ');
					writer.WriteLine(line);
				}
			}
			else
			{
				writer.Write(level);
				writer.Write(" [");
				writer.Write(loggingEvent.LoggerName);
				writer.Write("] ");
				writer.WriteLine(msg);
			}
		}

		private static string FormatLevel(Level level)
		{
			if (level == Level.Trace)
				return "[DEBUG]";

			if (level == Level.Warn)
				return "[WARN ]";

			if (level == Level.Error)
				return "[ERROR]";

			return "[INFO ]";
		}
	}
}