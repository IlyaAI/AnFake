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
		private bool _insideScript;

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
				DoFormat();				
				return _insideScript ? null : _stackTrace;
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

			var stackTrace = new StackTrace(InnerException ?? this, true);
			_insideScript = false;

			if (MyBuild.Current != null && MyBuild.Current.ScriptFile != null)
			{
				var frames = stackTrace.GetFrames();
				if (frames != null)
				{
					var scriptName = MyBuild.Current.ScriptFile.Name;
					var scriptFrame = frames.FirstOrDefault(f =>
					{
						var file = f.GetFileName();
						return file != null && file.EndsWith(scriptName, StringComparison.OrdinalIgnoreCase);
					});

					if (scriptFrame != null)
					{
						msgBuilder.Append(" @@ ").Append(scriptName).Append(' ').Append(scriptFrame.GetFileLineNumber());
						_insideScript = true;
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

			return _insideScript
				? _formattedMessage
				: _formattedMessage + Environment.NewLine + _stackTrace;
		}
	}
}