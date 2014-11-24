using System;

namespace AnFake.Core.Exceptions
{
	public sealed class AnFakeWrapperException : AnFakeException
	{
		public AnFakeWrapperException(Exception innerException)
			: base(String.Empty, innerException)
		{
		}
	}
}