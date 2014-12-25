using System;

namespace AnFake.Core.Exceptions
{
	public class TargetFailureException : AnFakeException
	{
		public TargetFailureException(string message) 
			: base(message)
		{
		}

		public TargetFailureException(string message, Exception innerException) 
			: base(message, innerException)
		{
		}

		public TargetFailureException(string message, string details)
			: base(message, details)
		{
		}
	}
}