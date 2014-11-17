using System;

namespace AnFake.Core.Exceptions
{
	public class TerminateTargetException : AnFakeException
	{
		public TerminateTargetException(string message) 
			: base(message)
		{
		}

		public TerminateTargetException(string message, Exception innerException) 
			: base(message, innerException)
		{
		}
	}
}