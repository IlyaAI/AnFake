using System;
using AnFake.Core.Exceptions;

namespace AnFake
{
	internal class EvaluationAbortedException : AnFakeException
	{
		public EvaluationAbortedException(Exception inner)
			: base("Script evaluation aborted.", inner)
		{
		}
	}
}