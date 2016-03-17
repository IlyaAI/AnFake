using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.Serialization;

namespace AnFake.Plugins.TeamCity.Rest
{
	[DebuggerDisplay("{Version}")]
	[DataContract]
	internal sealed class Revision
	{
		[DataMember(Name = "version")]
		public string Version { get; set; }		
	}

	[DataContract]
	internal sealed class RevisionsList
	{
		private readonly List<Revision> _items = new List<Revision>();

		[DataMember(Name = "count")]
		public int Count
		{
			get { return _items.Count; }
			set { /* do nothing */ }
		}

		[DataMember(Name = "revision")]
		public List<Revision> Items
		{
			get { return _items; }
		}
	}
}