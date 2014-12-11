using System.IO;
using AnFake.Core;
using log4net.Core;
using log4net.Layout;

namespace AnFake.Logging
{
	internal sealed class ConsolePattern : LayoutSkeleton
	{
		private readonly int _width;

		public ConsolePattern(int width)
		{
			_width = width;
		}

		public override void ActivateOptions()
		{
		}

		public override void Format(TextWriter writer, LoggingEvent loggingEvent)
		{
			var msg = loggingEvent.RenderedMessage;
			if (msg == null)
				return;

			foreach (var line in msg.GetLines())
			{
				if (line.Length >= _width - 4 && loggingEvent.Level < Level.Warn)
				{
					writer.Write(line.Substring(0, _width - 4));
					writer.WriteLine("...");
				}
				else
				{
					writer.WriteLine(line);
				}
			}
		}
	}
}