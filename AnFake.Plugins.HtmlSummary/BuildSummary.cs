using System.Collections.Generic;
using System.Linq;
using AnFake.Api;

namespace AnFake.Plugins.HtmlSummary
{	
	public sealed class BuildSummary
	{
		public const int PreviewCount = 3;

		public sealed class RequestedTarget
		{
			private readonly List<ExecutedTarget> _executedTargets = new List<ExecutedTarget>();

			public string Name { get; set; }

			public string State { get; set; }

			public List<ExecutedTarget> ExecutedTargets
			{
				get { return _executedTargets; }
			}
		}

		public sealed class ExecutedTarget
		{
			public string Name { get; set; }

			public string State { get; set; }

			public string RunTime { get; set; }

			public IEnumerable<TraceMessage> Messages { get; set; }

			public IEnumerable<TraceMessage> ErrorsAll
			{
				get { return Messages.Where(x => x.Level == TraceMessageLevel.Error); }
			}

			public IEnumerable<TraceMessage> ErrorsPreview
			{
				get { return ErrorsAll.Take(PreviewCount); }
			}

			public bool HasMoreErrors
			{
				get { return ErrorsAll.Take(PreviewCount + 1).Count() > PreviewCount; }
			}

			public IEnumerable<TraceMessage> WarningsAll
			{
				get { return Messages.Where(x => x.Level == TraceMessageLevel.Warning); }
			}

			public IEnumerable<TraceMessage> WarningsPreview
			{
				get { return WarningsAll.Take(PreviewCount); }
			}

			public bool HasMoreWarnings
			{
				get { return WarningsAll.Take(PreviewCount + 1).Count() > PreviewCount; }
			}

			public IEnumerable<TraceMessage> Summaries
			{
				get { return Messages.Where(x => x.Level == TraceMessageLevel.Summary); }
			}			
		}

		private readonly List<RequestedTarget> _requestedTargets = new List<RequestedTarget>();

		public string Status { get; set; }
		
		public string AgentName { get; set; }
		
		public string Changeset { get; set; }

		public string WorkingFolder { get; set; }

		public string LogFile { get; set; }
		
		public string RunTime { get; set; }
				
		public List<RequestedTarget> RequestedTargets
		{
			get { return _requestedTargets; }
		}
	}
}