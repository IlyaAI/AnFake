using System;

namespace AnFake.Core
{
	public class TargetFailureException : Exception
	{
		public TargetFailureException(string message) 
			: base(message)
		{
		}

		public TargetFailureException(string message, Exception innerException) 
			: base(message, innerException)
		{
		}
	}
}