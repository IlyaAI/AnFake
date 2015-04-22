using System;
using System.Linq;
using AnFake.Api.Pipeline;
using Microsoft.TeamFoundation.Build.Client;
using Microsoft.TeamFoundation.Build.Workflow;

namespace AnFake.Integration.Tfs2012.Pipeline
{
	internal sealed class TfsPipelineBuild : IPipelineBuild
	{
		private readonly IBuildDefinition _buildDef;
		private IQueuedBuild _queuedBuild;

		public TfsPipelineBuild(IBuildDefinition buildDef)
		{
			_buildDef = buildDef;
		}

		public Uri Uri
		{
			get
			{
				return _queuedBuild != null && _queuedBuild.Build != null
					? _queuedBuild.Build.Uri
					: _buildDef.Uri;
			}
		}

		public string Name
		{
			get
			{
				return _queuedBuild != null && _queuedBuild.Build != null
					? _queuedBuild.Build.BuildNumber
					: _buildDef.Name;
			}
		}

		public PipelineStepStatus Status
		{
			get
			{
				if (_queuedBuild == null)
					return PipelineStepStatus.None;

				_queuedBuild.Refresh(QueryOptions.None);
				
				if ((_queuedBuild.Status & QueueStatus.Queued) == QueueStatus.Queued)
					return PipelineStepStatus.Queued;

				if ((_queuedBuild.Status & QueueStatus.Canceled) == QueueStatus.Canceled)				
					return PipelineStepStatus.Failed;

				if ((_queuedBuild.Status & QueueStatus.Completed) == QueueStatus.Completed)
				{
					var details = _queuedBuild.Build;

					if ((details.Status & BuildStatus.PartiallySucceeded) == BuildStatus.PartiallySucceeded)
						return PipelineStepStatus.PartiallySucceeded;

					if ((details.Status & BuildStatus.Succeeded) == BuildStatus.Succeeded)
						return PipelineStepStatus.Succeeded;

					return PipelineStepStatus.Failed;
				}

				return PipelineStepStatus.InProgress;
			}
		}

		public TimeSpan WaitTime
		{
			get
			{
				if (_queuedBuild == null)
					return TimeSpan.Zero;

				return _queuedBuild.Build != null
					? _queuedBuild.Build.StartTime - _queuedBuild.QueueTime
					: DateTime.UtcNow - _queuedBuild.QueueTime;
			}
		}

		public TimeSpan RunTime
		{
			get
			{
				if (_queuedBuild == null || _queuedBuild.Build == null)
					return TimeSpan.Zero;

				return (_queuedBuild.Status & QueueStatus.InProgress) != QueueStatus.InProgress
					? _queuedBuild.Build.FinishTime - _queuedBuild.Build.StartTime
					: DateTime.UtcNow - _queuedBuild.Build.StartTime;
			}
		}

		public void EnsureInputSupported()
		{			
			// throw new InvalidOperationException(
			//	String.Format("Build '{0}' doesn't support input from previous pipeline step. Hint: ...", Name));
		}

		public void EnsureOutputSupported()
		{
			if (String.IsNullOrEmpty(_buildDef.DefaultDropLocation))
				throw new InvalidOperationException(
					String.Format("Build '{0}' doesn't support output which might be used by futher pipeline step. Hint: ensure drop location set properly.", Name));
		}

		public void Queue(IBuildServer buildSvc, string sourcesVersion, string requestedFor, IPipelineBuild input, string[] @params)
		{			
			var buildRequest = buildSvc.CreateBuildRequest(_buildDef.Uri);

			buildRequest.GetOption = GetOption.Custom;
			buildRequest.CustomGetVersion = sourcesVersion;
			
			if (requestedFor != null)
			{
				buildRequest.RequestedFor = requestedFor;
			}
			
			if (input != null || (@params != null && @params.Length > 0))
			{
				var processParams = WorkflowHelpers.DeserializeProcessParameters(_buildDef.ProcessParameters);

				object anfakeProps;
				if (!processParams.TryGetValue("AnFakeProperties", out anfakeProps))
				{
					anfakeProps = "";
				}
				
				if (input != null)
				{
					anfakeProps += " " + String.Format("{0}={1}", "Tfs.PipeIn", input.Uri);
				}
				if (@params != null && @params.Length > 0)
				{
					anfakeProps += " " + String.Join(" ", @params.Select(p => String.Format("{0}", p)));
				}

				processParams["AnFakeProperties"] = anfakeProps;

				buildRequest.ProcessParameters = WorkflowHelpers.SerializeProcessParameters(processParams);				
			}
			
			_queuedBuild = buildSvc.QueueBuild(buildRequest);
		}
	}
}