using System;
using System.Collections.Generic;
using System.Linq;
using AnFake.Api.Pipeline;
using Microsoft.TeamFoundation.Build.Client;
using Microsoft.TeamFoundation.Build.Workflow;

namespace AnFake.Integration.Tfs2012.Pipeline
{
	public sealed class TfsPipeline : IPipeline
	{
		private const string SummaryKey = "AnFakeSummary";
		private const string SummaryHeader = "AnFake Pipeline Summary";
		private const int SummaryPriority = 199;

		private readonly IDictionary<string, int> _aliases = new Dictionary<string, int>();
		private readonly ISet<int> _finishedBuilds = new HashSet<int>();
		private readonly IBuildDetail _currentBuild;
		private readonly IBuildInformation _tracker;
		private readonly IBuildServer _buildSvc;
		private bool _idle;

		public TfsPipeline(IBuildDetail currentBuild, IActivityTracking tracking)
		{
			_currentBuild = currentBuild;
			_tracker = tracking.Node.Children;
			_buildSvc = currentBuild.BuildServer;
		}		

		public PipelineStepStatus GetBuildStatus(object instanceId)
		{
			var id = (int) instanceId;
			if (id == 0)
				return PipelineStepStatus.Failed;

			var queuedBuild = _buildSvc.GetQueuedBuild(id, QueryOptions.All);

			if ((queuedBuild.Status & QueueStatus.Canceled) == QueueStatus.Canceled)
			{
				if (_finishedBuilds.Add(id))
				{
					var msg = String.Format(
						"{0} --:--:--  {1}", 
						queuedBuild.BuildDefinition.Name,
						PipelineStepStatus.Failed.ToUpperHumanReadable());

					LogError(msg);
					LogSummary(msg);
				}

				return PipelineStepStatus.Failed;
			}

			if ((queuedBuild.Status & QueueStatus.Completed) == QueueStatus.Completed)
			{
				var details = queuedBuild.Build;
				var status = PipelineStepStatus.Failed;

				if ((details.Status & BuildStatus.PartiallySucceeded) == BuildStatus.PartiallySucceeded)
				{
					status = PipelineStepStatus.PartiallySucceeded;
				}
				else if ((details.Status & BuildStatus.Succeeded) == BuildStatus.Succeeded)
				{
					status = PipelineStepStatus.Succeeded;
				}

				if (_finishedBuilds.Add(id))
				{
					var msg = String.Format(
						@"[{0}]({1}) {2:hh\:mm\:ss}  {3}",
						queuedBuild.Build.BuildNumber,
						queuedBuild.Build.Uri,
						queuedBuild.Build.FinishTime - queuedBuild.QueueTime,
						status.ToUpperHumanReadable());

					LogInfo(msg);
					LogSummary(msg);
				}

				return status;
			}

			if (!_idle)
			{
				LogInfo("Waiting...");
				_idle = true;
			}

			return PipelineStepStatus.InProgress;
		}

		public object StartBuild(string buildName, string pipeIn, string pipeOut, IDictionary<string, object> parameters)
		{
			var buildDef = _buildSvc.GetBuildDefinition(_currentBuild.TeamProject, buildName);
			var buildRequest = _buildSvc.CreateBuildRequest(buildDef.Uri);

			buildRequest.GetOption = GetOption.Custom;
			buildRequest.CustomGetVersion = _currentBuild.SourceGetVersion;

			var requestForCurrent = _currentBuild.Requests.FirstOrDefault();
			if (requestForCurrent != null)
			{
				buildRequest.RequestedFor = requestForCurrent.RequestedFor;
			}
			
			if (pipeIn != null)
			{
				int pipeInId;
				if (_aliases.TryGetValue(pipeIn, out pipeInId))
				{
					var input = _buildSvc.GetQueuedBuild(pipeInId, QueryOptions.All);

					if ((input.Status & QueueStatus.Completed) != QueueStatus.Completed)
					{
						LogErrorFormat(
							"'{0}' references '{1}' which isn't yet completed. Hint: try sequential run operator '->'.",
							buildName,
							input.BuildDefinition.Name);

						return 0;
					}

					if ((input.Build.Status & BuildStatus.Succeeded) != BuildStatus.Succeeded
						&& (input.Build.Status & BuildStatus.PartiallySucceeded) != BuildStatus.PartiallySucceeded)
					{
						LogErrorFormat(
							"'{0}' references '{1}' which has failed.",
							buildName,
							input.BuildDefinition.Name);

						return 0;
					}

					var processParams = WorkflowHelpers.DeserializeProcessParameters(buildDef.ProcessParameters);

					object anfakeProps;
					if (!processParams.TryGetValue("AnFakeProperties", out anfakeProps))
					{
						anfakeProps = "";
					}
					else
					{
						anfakeProps += " ";
					}
					anfakeProps += String.Format("{0}={1}", "Tfs.PipeIn", input.Build.Uri);

					processParams["AnFakeProperties"] = anfakeProps;

					buildRequest.ProcessParameters = WorkflowHelpers.SerializeProcessParameters(processParams);
				}
			}
			
			var queuedBuild = _buildSvc.QueueBuild(buildRequest);

			if (pipeOut != null)
			{
				_aliases.Add(pipeOut, queuedBuild.Id);
			}			
			
			LogInfoFormat("{0} @ {1} STARTED", queuedBuild.BuildDefinition.Name, queuedBuild.CustomGetVersion);
			_idle = false;

			return queuedBuild.Id;
		}

		public void LogInfo(string message)
		{
			_tracker.AddBuildMessage(message, BuildMessageImportance.Normal, DateTime.Now);
			_tracker.Save();
		}

		public void LogInfoFormat(string format, params object[] args)
		{
			LogInfo(String.Format(format, args));			
		}

		public void LogError(string message)
		{
			_tracker.AddBuildError(message, DateTime.Now);
			_tracker.Save();
		}

		public void LogErrorFormat(string format, params object[] args)
		{
			LogError(String.Format(format, args));
		}

		public void LogSummary(string message)
		{
			_currentBuild.Information
				.AddCustomSummaryInformation(message, SummaryKey, SummaryHeader, SummaryPriority);
			_currentBuild.Information
				.Save();
		}				
	}
}