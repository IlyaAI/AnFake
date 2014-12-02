using System.IO;
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
			var start = 0;			
			do
			{
				var index = msg.IndexOf('\n', start);
				if (index < 0) index = msg.Length;

				writer.Write(level);
				writer.Write(' ');
				writer.WriteLine(msg.Substring(start, index - start));				

				if (index + 1 < msg.Length && msg[index + 1] == '\r') index++;

				start = index + 1;
			} while (start < msg.Length);
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