using System;
using System.Collections.Generic;
using System.Linq;
using AnFake.Api;

namespace AnFake.Plugins.HtmlSummary
{	
	public sealed class BuildSummary
	{
		public const int PreviewCount = 2;

		public sealed class RequestedTarget
		{
			private readonly List<ExecutedTarget> _executedTargets = new List<ExecutedTarget>();

			public string Name { get; set; }

			public string State { get; set; }

			public bool HasErrorsOrWarnings
			{
				get { return ExecutedTargets.Any(x => x.HasErrorsOrWarnings); }
			}

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

			public bool HasErrorsOrWarnings
			{
				get { return Messages.Any(x => x.Level == TraceMessageLevel.Error || x.Level == TraceMessageLevel.Warning); }
			}

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

			public int ErrorsCount
			{
				get { return ErrorsAll.Count(); }
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

			public int WarningsCount
			{
				get { return WarningsAll.Count(); }
			}

			public IEnumerable<TraceMessage> Summaries
			{
				get { return Messages.Where(x => x.Level == TraceMessageLevel.Summary); }
			}

			public int SummariesCount
			{
				get { return Summaries.Count(); }
			}
		}

		private readonly List<RequestedTarget> _requestedTargets = new List<RequestedTarget>();

		public string Status { get; set; }
		
		public string AgentName { get; set; }
		
		public string Changeset { get; set; }

		public Uri WorkingFolderUri { get; set; }

		public Uri LogFileUri { get; set; }		
		
		public string RunTime { get; set; }

		public bool HasErrorsOrWarnings
		{
			get { return RequestedTargets.Any(x => x.HasErrorsOrWarnings); }
		}
				
		public List<RequestedTarget> RequestedTargets
		{
			get { return _requestedTargets; }
		}
	}
}