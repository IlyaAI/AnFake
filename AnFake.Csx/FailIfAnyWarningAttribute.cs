using System;

namespace AnFake.Csx
{
	[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
	public sealed class FailIfAnyWarningAttribute : Attribute
	{
	}
}
