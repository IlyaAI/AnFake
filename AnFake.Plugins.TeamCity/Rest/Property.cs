using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.Serialization;

namespace AnFake.Plugins.TeamCity.Rest
{
	[DebuggerDisplay("{Name} = {Value}")]
	[DataContract]
	internal sealed class Property
	{
		[DataMember(Name = "name")]
		public string Name { get; set; }

		[DataMember(Name = "value")]
		public string Value { get; set; }
	}

	[DataContract]
	internal sealed class PropertiesList
	{
		private readonly List<Property> _items = new List<Property>();

		[DataMember(Name = "count")]
		public int Count
		{
			get { return _items.Count; }
			set { /* do nothing */ }
		}

		[DataMember(Name = "property")]
		public List<Property> Items
		{
			get { return _items; }
		}
	}
}