using System;

namespace AnFake.Plugins.NHibernate
{
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
	public sealed class LengthAttribute : Attribute
	{
		public LengthAttribute(int value)
		{
			Value = value;
		}

		public int Value { get; private set; }		
	}
}