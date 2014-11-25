using System;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace AnFake.Core.Exceptions
{
	public abstract class AnFakeException : Exception
	{
		private string _message;
		private string _details;
		private bool _insideScript;

		protected AnFakeException(string message) : base(message)
		{
		}

		protected AnFakeException(string message, Exception innerException) : base(message, innerException)
		{
		}

		public override string Message
		{
			get
			{
				DoFormat();
				return _message;
			}
		}

		public override string StackTrace
		{
			get
			{
				DoFormat();				
				return _details;
			}
		}

		private void DoFormat()
		{
			if (_message != null)
				return;

			var msgBuilder = new StringBuilder();
			msgBuilder.Append(InnerException != null ? InnerException.Message : base.Message);

			var stackTrace = new StackTrace(InnerException ?? this, true);
			_insideScript = false;

			if (MyBuild.Defaults != null && MyBuild.Defaults.ScriptFile != null)
			{
				var frames = stackTrace.GetFrames();
				if (frames != null)
				{
					var scriptName = MyBuild.Defaults.ScriptFile.Name;
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

			_message = msgBuilder.ToString();
			_details = stackTrace.ToString();
		}

		public override string ToString()
		{
			DoFormat();

			return _insideScript
				? _message
				: _message + Environment.NewLine + _details;
		}
	}
}