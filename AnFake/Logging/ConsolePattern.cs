using System.IO;
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

			var start = 0;			
			do
			{
				var index = msg.IndexOf('\n', start);
				if (index < 0) index = msg.Length;

				if (index - start >= _width - 4)
				{
					writer.Write(msg.Substring(start, _width - 4));
					writer.WriteLine("...");
				}
				else
				{
					writer.WriteLine(msg.Substring(start, index - start));
				}

				if (index + 1 < msg.Length && msg[index + 1] == '\r') index++;

				start = index + 1;				

			} while (start < msg.Length);
		}
	}
}