using System;
using System.Activities;
using System.IO;
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

		public InArgument<string> Properties { get; set; }

		public InArgument<string> Script { get; set; }

		public InArgument<string> ToolPath { get; set; }

		public InvokeAnFake()
		{
			Verbosity = new InArgument<BuildVerbosity>(BuildVerbosity.Normal);
			Properties = new InArgument<string>("");
			Script = new InArgument<string>("build.fsx");
			ToolPath = new InArgument<string>(@".AnFake\Bin\AnFake.exe");

			Implementation = CreateBody;
		}

		private Activity CreateBody()
		{
			return new InvokeProcess
			{
				FileName = new InArgument<string>(ctx => Path.Combine(BuildDirectory.Get(ctx), ToolPath.Get(ctx))),
				Arguments = new InArgument<string>(
					ctx => String.Format("{0} {1} Verbosity={2} \"Tfs.Uri={3}\" \"Tfs.BuildUri={4}\" \"Tfs.ActivityInstanceId={5}\" {6}",
						Script.Get(ctx),
						Targets.Get(ctx),
						Verbosity.Get(ctx),
						BuildDetail.Get(ctx).BuildServer.TeamProjectCollection.Uri,
						BuildDetail.Get(ctx).Uri,
						ctx.ActivityInstanceId,
						Properties.Get(ctx))),
				WorkingDirectory = new InArgument<string>(ctx => BuildDirectory.Get(ctx))
			};
		}
	}
}