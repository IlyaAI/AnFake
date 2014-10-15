using System;

namespace AnFake.Core
{
	public class TerminateTargetException : Exception
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