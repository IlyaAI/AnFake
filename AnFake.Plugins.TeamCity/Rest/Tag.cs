using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.Serialization;

namespace AnFake.Plugins.TeamCity.Rest
{
	[DebuggerDisplay("{Name}")]
	[DataContract]
	internal sealed class Tag
	{
		public Tag()
		{			
		}

		public Tag(string name)
		{
			Name = name;
		}

		[DataMember(Name = "name")]
		public string Name { get; set; }
	}

	[DataContract]
	internal sealed class TagsList
	{
		private readonly List<Tag> _items;

		public TagsList()
		{
			_items = new List<Tag>();
		}

		public TagsList(List<Tag> items)
		{
			_items = items;
		}

		public TagsList(Tag item)
		{
			_items = new List<Tag> { item };
		}

		[DataMember(Name = "count")]
		public int Count
		{
			get { return _items.Count; }
			set { /* do nothing */ }
		}

		[DataMember(Name = "tag")]
		public List<Tag> Items
		{
			get { return _items; }
		}
	}
}