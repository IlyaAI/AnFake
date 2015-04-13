using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using AnFake.Api;
using AnFake.Api.Pipeline;
using Microsoft.TeamFoundation.Build.Client;

namespace AnFake.Integration.Tfs2012.Pipeline
{
	internal sealed class TfsPipelineRunner : IDisposable
	{
		private const string SummaryKey = "AnFakeSummary";
		private const string SummaryHeader = "AnFake Pipeline Summary";
		private const int SummaryPriority = 199;

		static TfsPipelineRunner()
		{
			Trace.Set(new BypassTracer());
		}

		private readonly IBuildDetail _currentBuild;
		private readonly IBuildInformation _tracker;		

		public TfsPipelineRunner(IBuildDetail currentBuild, string activityInstanceId)
		{
			_currentBuild = currentBuild;

			var activity = InformationNodeConverters.GetActivityTracking(_currentBuild, activityInstanceId);
			if (activity == null)
				throw new AnFakeBuildProcessException("TfsPipelineRunner unable to find activity with InstanceId='{0}'", activityInstanceId);

			_tracker = activity.Node.Children;			
			
			Trace.MessageReceived += OnMessageReceived;
		}

		public void Dispose()
		{
			Trace.MessageReceived -= OnMessageReceived;			
		}

		public void Run(string pipelineDef, TimeSpan spinTime, TimeSpan timeout, CancellationToken cancellationToken)
		{
			var finalStatus = PipelineStepStatus.None;
			var pipeline = (Api.Pipeline.Pipeline) null;

			try
			{
				pipeline = new Api.Pipeline.Pipeline(
					pipelineDef,
					new TfsPipelineImplementor(_currentBuild));

				finalStatus = pipeline.Run(spinTime, timeout, cancellationToken);
			}
			catch (Exception e)
			{
				Trace.Error(e.Message);

				finalStatus = PipelineStepStatus.Failed;
			}

			Summarize(
				pipelineDef, 
				finalStatus, 
				pipeline != null 
					? pipeline.TriggeredBuilds 
					: Enumerable.Empty<IPipelineBuild>());

			switch (finalStatus)
			{
				case PipelineStepStatus.Succeeded:
					_currentBuild.FinalizeStatus(BuildStatus.Succeeded);
					break;

				case PipelineStepStatus.PartiallySucceeded:
					_currentBuild.FinalizeStatus(BuildStatus.PartiallySucceeded);
					break;

				default:
					_currentBuild.FinalizeStatus(BuildStatus.Failed);
					break;
			}
		}

		private void Summarize(string pipelineDef, PipelineStepStatus finalStatus, IEnumerable<IPipelineBuild> triggeredBuilds)
		{
			WriteSummary(
				new TfsMessageBuilder()
					.Append("Sources Version: ").Append(_currentBuild.SourceGetVersion));
			WriteSummary(
				new TfsMessageBuilder()
					.Append("Pipeline: ").Append(pipelineDef));			
			WriteSummary("");

			foreach (var build in triggeredBuilds)
			{
				WriteSummary(
					new TfsMessageBuilder()
						.AppendLink(build.Name, build.Uri)
						.AppendFormat(@"  W {0:hh\:mm\:ss}  R {1:hh\:mm\:ss}  ", build.WaitTime, build.RunTime)
						.Append(build.Status.ToUpperHumanReadable()));
			}

			WriteSummary(new string('=', 48));
			WriteSummary(
				new TfsMessageBuilder()
					.Append("PIPELINE ").Append(finalStatus.ToUpperHumanReadable()));

			SaveSummary();
		}

		private void OnMessageReceived(object sender, TraceMessage message)
		{
			if (message.ThreadId != Thread.CurrentThread.ManagedThreadId)
				return;

			_tracker.TraceMessage(message, true);
			_tracker.Save();
		}

		private void WriteSummary(string message)
		{
			_currentBuild.Information
				.AddCustomSummaryInformation(message, SummaryKey, SummaryHeader, SummaryPriority);
		}

		private void WriteSummary(TfsMessageBuilder builder)
		{
			WriteSummary(builder.ToString());
		}

		private void SaveSummary()
		{
			_currentBuild.Information.Save();
		}
	}
}