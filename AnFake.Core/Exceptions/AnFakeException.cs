using System;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace AnFake.Core.Exceptions
{
	public abstract class AnFakeException : Exception
	{
		private readonly string _details;
		private string _formattedMessage;
		private string _stackTrace;		

		protected AnFakeException(string message) : base(message)
		{
		}

		protected AnFakeException(string message, Exception innerException) : base(message, innerException)
		{
		}

		protected AnFakeException(string message, string details)
			: base(message)
		{
			_details = details;
		}

		public override string Message
		{
			get
			{
				DoFormat();
				return _formattedMessage;
			}
		}

		public override string StackTrace
		{
			get
			{
				if (StackTraceMode != StackTraceMode.Full) 
					return null;

				DoFormat();
				return _stackTrace;
			}
		}

		public string OriginalStackTrace
		{
			get
			{
				DoFormat();
				return _stackTrace;
			}
		}

		public string Details
		{
			get { return _details; }
		}

		private void DoFormat()
		{
			if (_formattedMessage != null)
				return;

			var msgBuilder = new StringBuilder();
			msgBuilder.Append(InnerException != null ? InnerException.Message : base.Message);

			var ex = InnerException ?? this;
			for (var level = 1; ex.InnerException != null; level++)
			{
				ex = ex.InnerException;
				msgBuilder
					.AppendLine()
					.Append('>', level).Append(' ')
					.Append(ex.Message);
			}
			
			var stackTrace = new StackTrace(InnerException ?? this, true);
			
			if (ScriptSource != null && ScriptSource.GeneratedName != null)
			{
				var frames = stackTrace.GetFrames();
				if (frames != null)
				{					
					var scriptFrame = frames.FirstOrDefault(f =>
					{
						var file = f.GetFileName();
						return file != null && file.EndsWith(ScriptSource.GeneratedName, StringComparison.OrdinalIgnoreCase);
					});

					if (scriptFrame != null)
					{
						msgBuilder.Append(" @@ ")
							.Append(ScriptSource.OriginalName)
							.Append(' ')
							.Append(scriptFrame.GetFileLineNumber() - ScriptSource.LinesOffset);
					}
				}
			}

			if (!String.IsNullOrEmpty(_details))
			{
				msgBuilder
					.AppendLine()					
					.Append(_details);
			}

			_formattedMessage = msgBuilder.ToString();
			_stackTrace = stackTrace.ToString();
		}

		public override string ToString()
		{
			DoFormat();

			return StackTraceMode == StackTraceMode.ScriptOnly
				? _formattedMessage
				: _formattedMessage + Environment.NewLine + _stackTrace;
		}

		public static StackTraceMode StackTraceMode { get; set; }

		public static ScriptSourceInfo ScriptSource { get; set; }		

		public static AnFakeException Wrap(Exception e)
		{
			return e as AnFakeException ?? new AnFakeWrapperException(e);
		}

		public static string ToString(Exception e)
		{
			return Wrap(e).ToString();
		}
	}
}