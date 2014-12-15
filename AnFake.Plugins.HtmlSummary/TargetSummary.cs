using System.Collections.Generic;
using System.Runtime.Serialization;
using AnFake.Api;
using AnFake.Core;

namespace AnFake.Plugins.HtmlSummary
{
	[DataContract]
	public sealed class TargetSummary
	{
		private IList<TraceMessage> _messages;
		private IList<TargetSummary> _children;

		[DataMember]
		public string Name { get; set; }

		[DataMember]
		public TargetState State { get; set; }

		[DataMember]
		public long RunTimeMs { get; set; }

		[DataMember]
		public int ErrorsCount { get; set; }

		[DataMember]
		public int WarningsCount { get; set; }

		[DataMember]
		public int MessagesCount { get; set; }

		[DataMember]
		public IList<TraceMessage> Messages
		{
			get { return _messages ?? (_messages = new List<TraceMessage>()); }
		}

		[DataMember]
		public IList<TargetSummary> Children
		{
			get { return _children ?? (_children = new List<TargetSummary>()); }
		}
	}
}