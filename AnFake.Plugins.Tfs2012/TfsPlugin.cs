using System;
using System.Linq;
using AnFake.Api;
using AnFake.Core;
using AnFake.Core.Exceptions;
using Common.Logging;
using Microsoft.TeamFoundation.Build.Client;
using Microsoft.TeamFoundation.Client;

namespace AnFake.Plugins.Tfs2012
{
	internal sealed class TfsPlugin : IPlugin
	{
		private static readonly ILog Log = LogManager.GetLogger<TfsPlugin>();

		private const string SectionKey = "AnFake";
		private const string SectionHeader = "AnFake Summary";

		private readonly TfsTeamProjectCollection _teamProjectCollection;

		private readonly IBuildDetail _build;
		private readonly IBuildInformation _tracker;

		public TfsPlugin(MyBuild.Params parameters)
		{
			string tfsUri;
			string buildUri;
			string activityInstanceId;

			if (!parameters.Properties.TryGetValue("Tfs.Uri", out tfsUri))
				throw new InvalidConfigurationException("TFS plugin requires 'Tfs.Uri' to be specified in build properties.");

			parameters.Properties.TryGetValue("Tfs.BuildUri", out buildUri);
			parameters.Properties.TryGetValue("Tfs.ActivityInstanceId", out activityInstanceId);

			_teamProjectCollection = TfsTeamProjectCollectionFactory.GetTeamProjectCollection(new Uri(tfsUri));

			if (!String.IsNullOrEmpty(buildUri))
			{
				if (String.IsNullOrEmpty(activityInstanceId))
					throw new InvalidConfigurationException("TFS plugin requires both 'Tfs.BuildUri' and 'Tfs.ActivityInstanceId' to be specified in build properties.");

				var buildServer = (IBuildServer)_teamProjectCollection.GetService(typeof(IBuildServer));

				_build = buildServer.QueryBuildsByUri(
					new[] { new Uri(buildUri) },
					new[] { "ActivityTracking" },
					QueryOptions.None).Single();

				if (_build == null)
					throw new InvalidConfigurationException(String.Format("TFS plugin unable to find build '{0}'", buildUri));

				var activity = InformationNodeConverters.GetActivityTracking(_build, activityInstanceId);
				if (activity == null)
					throw new InvalidConfigurationException(String.Format("TFS plugin unable to find Activity with InstanceId='{0}'", activityInstanceId));
				
				_tracker = activity.Node.Children;

				parameters.Tracer.MessageReceived += OnMessage;
				Target.RunFinished += OnRunFinished;
			}
			else
			{
				_build = null;
				_tracker = null;
			}
		}

		public TfsTeamProjectCollection TeamProjectCollection
		{
			get { return _teamProjectCollection; }
		}

		public IBuildDetail Build
		{
			get
			{
				if (_build == null)
					throw new InvalidConfigurationException("Build details are unavailable. Hint: you should specify 'Tfs.BuildUri' and 'Tfs.ActivityInstanceId' in build properties.");

				return _build;
			}
		}

		private void OnMessage(object sender, TraceMessage message)
		{
			switch (message.Level)
			{
				case TraceMessageLevel.Debug:
					_tracker.AddBuildMessage(message.ToString(), BuildMessageImportance.Low, DateTime.Now);					
					break;

				case TraceMessageLevel.Info:
					_tracker.AddBuildMessage(message.ToString(), BuildMessageImportance.Normal, DateTime.Now);
					break;

				case TraceMessageLevel.Summary:
					_tracker.AddBuildMessage(message.ToString(), BuildMessageImportance.High, DateTime.Now);
					break;

				case TraceMessageLevel.Warning:
					_tracker.AddBuildWarning(message.ToString(), DateTime.Now);
					break;

				case TraceMessageLevel.Error:
					_tracker.AddBuildError(message.ToString(), DateTime.Now);
					break;
			}

			_tracker.Save();
		}

		private void OnRunFinished(object sender, Target.RunFinishedEventArgs evt)
		{
			string summary;
			var hasErrorsOrWarns = false;
			var currentTarget = (Target) sender;

			foreach (var target in evt.ExecutedTargets)
			{
				summary = String.Format("{0}: {1,3} error(s) {2,3} warning(s) {3,3} messages(s)  {4}",
					target.Name, target.Messages.ErrorsCount, target.Messages.WarningsCount, target.Messages.SummariesCount, 
					target.State.ToHumanReadable().ToUpperInvariant());

				_build.Information
					.AddCustomSummaryInformation(summary, SectionKey, SectionHeader, 199);

				foreach (var message in target.Messages.Where(x => x.Level == TraceMessageLevel.Summary))
				{
					summary = String.IsNullOrEmpty(message.LinkHref)
						? String.Format("    {0}", message.Message)
						: String.Format("    {0} [{1}]({2})", message.Message, message.LinkLabel ?? message.LinkHref, message.LinkHref);					

					_build.Information
						.AddCustomSummaryInformation(summary, SectionKey, SectionHeader, 199);
				}

				hasErrorsOrWarns |= 
					target.Messages.ErrorsCount > 0 || 
					target.Messages.WarningsCount > 0;
			}

			summary = String.Format("'{0}' {1}", currentTarget.Name, evt.FinalState.ToHumanReadable().ToUpperInvariant());
			if (!String.IsNullOrEmpty(_build.DropLocation))
			{
				summary += String.Format("  [build.log]({0})", _build.DropLocation.AsPath() / MyBuild.Defaults.LogFile.RelPath);
			}

			_build.Information
				.AddCustomSummaryInformation(new string('=', 48), SectionKey, SectionHeader, 199);
			_build.Information
				.AddCustomSummaryInformation(summary, SectionKey, SectionHeader, 199);

			if (hasErrorsOrWarns)
			{
				_build.Information
					.AddCustomSummaryInformation("See the section below for error/warning list.", SectionKey, SectionHeader, 199);
			}			

			_build.Information
				.Save();

			if (currentTarget.Name == MyBuild.Defaults.Targets.Last())
			{
				_build.FinalizeStatus(AsTfsBuildStatus(evt.FinalState));
			}
			else
			{
				_build.Status = AsTfsBuildStatus(evt.FinalState);
				_build.Save();
			}
		}

		private static BuildStatus AsTfsBuildStatus(TargetState state)
		{
			switch (state)
			{
				case TargetState.Succeeded:
					return BuildStatus.Succeeded;

				case TargetState.PartiallySucceeded:
					return BuildStatus.PartiallySucceeded;

				case TargetState.Failed:
					return BuildStatus.Failed;

				default:
					throw new InvalidOperationException(
						String.Format("Inconsistency detected: final state expected to be {{Successed|PartiallySuccessed|Failed}} but {0} provided.", state));
			}
		}
	}
}