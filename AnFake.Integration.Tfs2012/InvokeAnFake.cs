using System;
using System.Activities;
using System.Activities.Statements;
using System.IO;
using AnFake.Api;
using Microsoft.TeamFoundation.Build.Client;
using Microsoft.TeamFoundation.Build.Workflow;
using Microsoft.TeamFoundation.Build.Workflow.Activities;
using Microsoft.TeamFoundation.Build.Workflow.Services;
using Microsoft.TeamFoundation.Build.Workflow.Tracking;

namespace AnFake.Integration.Tfs2012
{
	[ActivityTracking(ActivityTrackingOption.ActivityOnly)]
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

		public InvokeAnFake()
		{
			Verbosity = new InArgument<BuildVerbosity>(BuildVerbosity.Normal);
			CleanWorkspace = new InArgument<CleanWorkspaceOption>(CleanWorkspaceOption.Outputs);
			PrivateDropLocation = new InArgument<string>();
			Properties = new InArgument<string>("");
			Script = new InArgument<string>("build.fsx");			

			Implementation = CreateBody;
		}

		protected override void CacheMetadata(ActivityMetadata metadata)
		{
			base.CacheMetadata(metadata);

			metadata.RequireExtension<IBuildLoggingExtension>();
		}

		private Activity CreateBody()
		{
			var retCode = new Variable<int>();
			var dummy = new Variable<int>();
			var outBuffer = new Variable<ProcessOutputBuffer>(ctx => new ProcessOutputBuffer());

			var invokeProcess = new InvokeProcess
			{				
				FileName = new InArgument<string>(ctx => Path.Combine(BuildDirectory.Get(ctx), @".AnFake\AnFake.exe")),
				Arguments = new InArgument<string>(
					ctx => new Args("", "=")
						.Param(Script.Get(ctx))
						.CommandIf("Clean", CleanWorkspace.Get(ctx) != CleanWorkspaceOption.None)
						.Command(Targets.Get(ctx))
						.CommandIf("Drop", !String.IsNullOrEmpty(BuildDetail.Get(ctx).DropLocationRoot) || !String.IsNullOrEmpty(PrivateDropLocation.Get(ctx)))
						.Option("Verbosity", Verbosity.Get(ctx))
						.Option("Tfs.Uri", BuildDetail.Get(ctx).BuildServer.TeamProjectCollection.Uri)
						.Option("Tfs.BuildUri", BuildDetail.Get(ctx).Uri)
						.Option("Tfs.ActivityInstanceId", ctx.GetExtension<IBuildLoggingExtension>().GetActivityTracking(ctx).ActivityInstanceId)
						.Option("Tfs.PrivateDropLocation", PrivateDropLocation.Get(ctx))
						.Other(Properties.Get(ctx))
						.ToString()),
				WorkingDirectory = new InArgument<string>(ctx => BuildDirectory.Get(ctx)),
				Result = new OutArgument<int>(retCode),
				OutputDataReceived = AppendBuffer(outBuffer, dummy),
				ErrorDataReceived = AppendBuffer(outBuffer, dummy)
			};

			return new Sequence
			{
				Variables = {retCode, dummy, outBuffer},
				Activities =
				{
					invokeProcess, 
					CheckExitCode(retCode, outBuffer)
				}
			};
		}

		/*private static string LocateAnFake(string buildDirectory)
		{
			var searchPath = Path.Combine(buildDirectory, "packages");
			var newestPackage = Directory.GetDirectories(
				searchPath,
				"AnFake.*", 
				SearchOption.TopDirectoryOnly)
					.OrderByDescending(x => new Version(x.Substring(7)))
					.FirstOrDefault();

			if (newestPackage == null)
				throw new AnFakeBuildProcessException("Unable to locate AnFake package in '{0}'.", searchPath);

			return Path.Combine(newestPackage, "AnFake.exe");
		}*/

		private static ActivityAction<string> AppendBuffer(Variable<ProcessOutputBuffer> outBuffer, Variable<int> dummy)
		{
			var arg = new DelegateInArgument<string>();
			return new ActivityAction<string>
			{
				Argument = arg,
				Handler = new Assign
				{
					To = new OutArgument<int>(dummy),
					Value = new InArgument<int>(ctx => outBuffer.Get(ctx).Append(arg.Get(ctx)))
				}
			};
		}

		private static If CheckExitCode(Variable<int> retCode, Variable<ProcessOutputBuffer> outBuffer)
		{
			return new If(ctx => retCode.Get(ctx) < 0 || retCode.Get(ctx) > 2)
			{
				Then = new Sequence
				{
					Activities =
					{
						new Throw
						{
							Exception = new InArgument<Exception>(
								ctx => new AnFakeBuildProcessException(
									"AnFake.exe has failed with exit code: {0}.\n" +
									"============= Process Output =============\n" +
									"{1}",
									retCode.Get(ctx),
									outBuffer.Get(ctx)))
						}						
					}
				}
			};
		}
	}
}