using System.IO;
using System.Text;
using Common.Logging;

namespace AnFake.Logging
{
	internal abstract class LogWriter : TextWriter
	{		
		private readonly StringBuilder _message = new StringBuilder(128);
		private readonly ILog _log;

		protected LogWriter(ILog log)
		{
			_log = log;
		}		

		public override Encoding Encoding
		{
			get { return Encoding.UTF8; }
		}

		public override void Write(char[] buffer, int index, int count)
		{
			var end = index + count;
			var br = index;

			while (br < end)
			{
				while (br < end && buffer[br] != '\r' && buffer[br] != '\n') br++;

				var len = br - index;
				if (len > 0)
				{
					_message.Append(buffer, index, len);
				}

				if (br < end && _message.Length > 0)
				{
					LogMessage(_log, _message.ToString());
					_message.Clear();
				}

				while (br < end && (buffer[br] == '\r' || buffer[br] == '\n')) br++;

				index = br;
			}			
		}

		protected abstract void LogMessage(ILog log, string message);
	}
}