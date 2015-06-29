using System;
using AnFake.Core.Exceptions;

namespace AnFake.Scripting
{
	internal sealed class EvaluationException : AnFakeException
	{
		public EvaluationException(string message) : base(message)
		{
		}

		public EvaluationException(string message, Exception innerException) : base(message, innerException)
		{
		}
	}
}