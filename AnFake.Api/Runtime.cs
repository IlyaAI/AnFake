using System;

namespace AnFake.Api
{
	public static class Runtime
	{
		public static bool IsMono
		{
			get { return Type.GetType("Mono.Runtime") != null; }
		}
	}
}