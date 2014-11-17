using System;

namespace AnFake.Core.Exceptions
{
	public sealed class InvalidConfigurationException : AnFakeException
	{
		public InvalidConfigurationException(string message) : base(message)
		{
		}

		public InvalidConfigurationException(string message, Exception innerException) : base(message, innerException)
		{
		}
	}
}