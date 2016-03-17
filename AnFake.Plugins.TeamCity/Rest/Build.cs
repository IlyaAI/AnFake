using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace AnFake.Plugins.TeamCity.Rest
{
	[DataContract]
	internal sealed class Build
	{
		[DataMember(Name = "number")]
		public string Number { get; set; }

		[DataMember(Name = "href")]
		public Uri Href { get; set; }

		[DataMember(Name = "webUrl")]
		public Uri WebUrl { get; set; }

		[DataMember(Name = "buildTypeId")]
		public string BuildTypeId { get; set; }

		[DataMember(Name = "startDate")]
		public string StartDate { get; set; }

		[DataMember(Name = "finishDate")]
		public string FinishDate { get; set; }

		[DataMember(Name = "revisions")]
		public RevisionsList Revisions { get; set; }
	}

	[DataContract]
	internal sealed class BuildsList
	{
		private readonly List<Build> _items = new List<Build>();
		
		[DataMember(Name = "count")]
		public int Count
		{
			get { return _items.Count; }
			set { /* do nothing */ }
		}

		[DataMember(Name = "build")]
		public List<Build> Items
		{
			get { return _items; }
		}
	}
}