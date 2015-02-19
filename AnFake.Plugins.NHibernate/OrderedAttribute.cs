using System;

namespace AnFake.Plugins.NHibernate
{
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
	public sealed class OrderedAttribute : Attribute
	{		
	}
}