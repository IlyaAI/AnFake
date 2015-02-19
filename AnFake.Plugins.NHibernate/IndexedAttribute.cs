using System;

namespace AnFake.Plugins.NHibernate
{
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
	public sealed class IndexedAttribute : Attribute
	{
		public IndexedAttribute(string name)
		{
			Name = name;
		}

		public string Name { get; private set; }

		public bool IsUnique { get; set; }
	}
}