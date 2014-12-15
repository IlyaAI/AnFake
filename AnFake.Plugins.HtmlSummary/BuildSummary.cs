using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using AnFake.Core;

namespace AnFake.Plugins.HtmlSummary
{
	[DataContract]
	public sealed class BuildSummary
	{
		private IList<TargetSummary> _targets;

		[DataMember]
		public MyBuild.Status Status { get; set; }

		[DataMember]
		public string ComputerName { get; set; }

		[DataMember]
		public string ChangesetId { get; set; }

		[DataMember]
		public string ChangesetAuthor { get; set; }

		[DataMember]
		public DateTime StartTime { get; set; }

		[DataMember]
		public DateTime FinishTime { get; set; }
		
		[DataMember]
		public IList<TargetSummary> Targets
		{
			get { return _targets ?? (_targets = new List<TargetSummary>()); }
		}
	}
}