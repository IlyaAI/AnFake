using System;
using System.Activities;
using System.IO;
using AnFake.Api;
using Microsoft.TeamFoundation.Build.Client;
using Microsoft.TeamFoundation.Build.Workflow;
using Microsoft.TeamFoundation.Build.Workflow.Activities;

namespace AnFake.Integration.Tfs2012
{
	[BuildActivity(HostEnvironmentOption.All)]
	public sealed class InvokeAnFake : Activity
	{
		[RequiredArgument]
		public InArgument<IBuildDetail> BuildDetail { get; set; }

		[RequiredArgument]
		public InArgument<string> BuildDirectory { get; set; }

		[RequiredArgument]
		public InArgument<string> Targets { get; set; }

		public InArgument<BuildVerbosity> Verbosity { get; set; }

		public InArgument<CleanWorkspaceOption> CleanWorkspace { get; set; }

		public InArgument<string> PrivateDropLocation { get; set; }

		public InArgument<string> Properties { get; set; }

		public InArgument<string> Script { get; set; }

		public InArgument<string> ToolPath { get; set; }

		public InvokeAnFake()
		{
			Verbosity = new InArgument<BuildVerbosity>(BuildVerbosity.Normal);
			CleanWorkspace = new InArgument<CleanWorkspaceOption>(CleanWorkspaceOption.Outputs);
			PrivateDropLocation = new InArgument<string>();
			Properties = new InArgument<string>("");
			Script = new InArgument<string>("build.fsx");
			ToolPath = new InArgument<string>(@".AnFake\AnFake.exe");

			Implementation = CreateBody;
		}

		private Activity CreateBody()
		{
			return new InvokeProcess
			{
				FileName = new InArgument<string>(ctx => Path.Combine(BuildDirectory.Get(ctx), ToolPath.Get(ctx))),
				Arguments = new InArgument<string>(
					ctx => new Args("", "=")
						.Param(Script.Get(ctx))
						.CommandIf("Clean", CleanWorkspace.Get(ctx) != CleanWorkspaceOption.None)
						.Command(Targets.Get(ctx))
						.CommandIf("Drop", !String.IsNullOrEmpty(BuildDetail.Get(ctx).DropLocationRoot) || !String.IsNullOrEmpty(PrivateDropLocation.Get(ctx)))
						.Option("Verbosity", Verbosity.Get(ctx))
						.Option("Tfs.Uri", BuildDetail.Get(ctx).BuildServer.TeamProjectCollection.Uri)
						.Option("Tfs.BuildUri", BuildDetail.Get(ctx).Uri)
						.Option("Tfs.ActivityInstanceId", ctx.ActivityInstanceId)
						.Option("Tfs.PrivateDropLocation", PrivateDropLocation.Get(ctx))
						.Other(Properties.Get(ctx))
						.ToString()),
				WorkingDirectory = new InArgument<string>(ctx => BuildDirectory.Get(ctx))
			};
		}
	}
}