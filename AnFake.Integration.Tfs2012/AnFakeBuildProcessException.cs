using System;

namespace AnFake.Integration.Tfs2012
{
	internal sealed class AnFakeBuildProcessException : Exception
	{
		public AnFakeBuildProcessException(string format, params object[] args)
			: base(String.Format(format, args))
		{
		}

		public override string ToString()
		{
			return Message;
		}
	}
}