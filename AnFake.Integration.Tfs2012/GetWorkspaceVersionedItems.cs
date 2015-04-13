using System;
using System.Activities;
using System.Linq;
using AnFake.Api;
using Microsoft.TeamFoundation.Build.Client;
using Microsoft.TeamFoundation.Build.Workflow.Services;
using Microsoft.TeamFoundation.VersionControl.Client;

namespace AnFake.Integration.Tfs2012
{
	[BuildActivity(HostEnvironmentOption.All)]
	public sealed class GetWorkspaceVersionedItems : AsyncCodeActivity
	{
		[RequiredArgument]
		public InArgument<Workspace> Workspace { get; set; }

		[RequiredArgument]
		public InArgument<string> Version { get; set; }

		[RequiredArgument]
		public InArgument<ExtendedMapping[]> ExtendedMappings { get; set; }

		protected override void CacheMetadata(CodeActivityMetadata metadata)
		{
			base.CacheMetadata(metadata);

			metadata.RequireExtension<IBuildLoggingExtension>();
		}

		protected override IAsyncResult BeginExecute(AsyncCodeActivityContext context, AsyncCallback callback, object state)
		{
			var workspace = Workspace.Get(context);
			var version = Version.Get(context);
			var mappings = ExtendedMappings.Get(context);

			var versionSpec = VersionSpec.Latest;
			if (!String.IsNullOrEmpty(version))
			{
				versionSpec = VersionSpec.ParseSingleSpec(version, ".");
			}

			var tracker = context
				.GetExtension<IBuildLoggingExtension>()
				.GetActivityTracking(context)
				.Node
				.Children;

			var doRun = new Action<Workspace, VersionSpec, ExtendedMapping[], IBuildInformation>(DoRun);
			context.UserState = doRun;

			return doRun.BeginInvoke(
				workspace,
				versionSpec,
				mappings,
				tracker,
				callback,
				state);
		}

		protected override void EndExecute(AsyncCodeActivityContext context, IAsyncResult result)
		{
			var doRun = (Action<Workspace, VersionSpec, ExtendedMapping[], IBuildInformation>) context.UserState;

			try
			{
				doRun.EndInvoke(result);
			}
			catch (OperationCanceledException)
			{
				// skip
			}
		}

		protected override void Cancel(AsyncCodeActivityContext context)
		{			
			var vcs = Workspace.Get(context).VersionControlServer;
			if (vcs == null)
				return;

			vcs.Canceled = true;
		}

		private static void DoRun(Workspace workspace, VersionSpec versionSpec, ExtendedMapping[] mappings, IBuildInformation tracker)
		{
			foreach (var mapping in mappings.Where(x => x.VersionSpec != null && x.VersionSpec != versionSpec))
			{
				tracker.TraceMessage(
					new TraceMessage(
						TraceMessageLevel.Info,
						String.Format("Getting '{0}' @ {1}...", mapping.ServerItem, mapping.VersionSpec.DisplayString)));

				var status = workspace.Get(
					new GetRequest(mapping.ServerItem, mapping.Depth, mapping.VersionSpec),
					GetOptions.Overwrite);

				foreach (var failure in status.GetFailures())
				{
					tracker.TraceMessage(
						new TraceMessage(TraceMessageLevel.Warning, failure.GetFormattedMessage()));
				}

				tracker.Save();
			}
		}
	}
}