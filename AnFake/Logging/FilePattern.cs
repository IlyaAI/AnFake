using System.IO;
using AnFake.Core;
using log4net.Core;
using log4net.Layout;

namespace AnFake.Logging
{
	internal sealed class FilePattern : LayoutSkeleton
	{
		public override void ActivateOptions()
		{
		}

		public override void Format(TextWriter writer, LoggingEvent loggingEvent)
		{
			var msg = loggingEvent.RenderedMessage;
			if (msg == null)
				return;

			var level = FormatLevel(loggingEvent.Level);
			foreach (var line in TextLine.From(msg))
			{
				writer.Write(level);
				writer.Write(' ');
				writer.WriteLine(line);
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