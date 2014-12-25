using System;
using System.Activities;
using System.Activities.Statements;
using System.Collections.Generic;
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
		private sealed class OutputBuffer
		{
			private readonly int _capacity;
			private readonly Queue<string> _buffer;

			public OutputBuffer(int capacity)
			{
				_capacity = capacity;
				_buffer = new Queue<string>(capacity);
			}

			// Used via Reflection
			// ReSharper disable once UnusedMember.Local
			public void Append(string data)
			{
				if (String.IsNullOrWhiteSpace(data))
					return;

				while (_buffer.Count >= _capacity)
					_buffer.Dequeue();

				_buffer.Enqueue(data);
			}

			public override string ToString()
			{
				return String.Join("\n", _buffer);
			}
		}

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

		protected override void CacheMetadata(ActivityMetadata metadata)
		{
			base.CacheMetadata(metadata);

			metadata.RequireExtension<IBuildLoggingExtension>();
		}

		private Activity CreateBody()
		{
			const int outputBufferCapacity = 48; // lines
			
			var retCode = new Variable<int>();
			var outBuffer = new Variable<OutputBuffer>(ctx => new OutputBuffer(outputBufferCapacity));

			var onOutData = new ActivityAction<string>
			{
				Argument = new DelegateInArgument<string>()
			};
			onOutData.Handler = new InvokeMethod
			{
				DisplayName = "OutputBuffer.Append",
				MethodName = "Append",
				Parameters = {new InArgument<string>(onOutData.Argument)},
				TargetObject = new InArgument<OutputBuffer>(outBuffer)
			};
			var onErrData = new ActivityAction<string>
			{
				Argument = new DelegateInArgument<string>()
			};
			onErrData.Handler = new InvokeMethod
			{				
				DisplayName = "OutputBuffer.Append",
				MethodName = "Append",
				Parameters = {new InArgument<string>(onErrData.Argument)},
				TargetObject = new InArgument<OutputBuffer>(outBuffer)
			};

			var invokeProcess = new InvokeProcess
			{
				DisplayName = "Invoke AnFake.exe",
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
						.Option("Tfs.ActivityInstanceId", ctx.GetExtension<IBuildLoggingExtension>().GetActivityTracking(ctx).ActivityInstanceId)
						.Option("Tfs.PrivateDropLocation", PrivateDropLocation.Get(ctx))
						.Other(Properties.Get(ctx))
						.ToString()),
				WorkingDirectory = new InArgument<string>(ctx => BuildDirectory.Get(ctx)),
				Result = new OutArgument<int>(retCode),
				OutputDataReceived = onOutData,
				ErrorDataReceived = onErrData
			};

			var checkExitCode = new If(ctx => retCode.Get(ctx) < 0 || retCode.Get(ctx) > 2)
			{
				DisplayName = "Check Exit Code",
				Then = new Sequence
				{
					Activities =
					{
						new WriteBuildError
						{
							Message = new InArgument<string>(
								ctx => String.Format(
									"AnFake.exe has failed with exit code: {0}.\n" +
									"============= Process Output =============\n" +
									"{1}",
									retCode.Get(ctx),
									outBuffer.Get(ctx)))
						},
						new SetBuildProperties
						{
							DisplayName = "Set 'Build Failed'",
							PropertiesToSet = BuildUpdate.Status,
							Status = new InArgument<BuildStatus>(ctx => BuildStatus.Failed)
						}
					}
				}
			};

			return new Sequence
			{
				Variables = { retCode, outBuffer },
				Activities = { invokeProcess, checkExitCode }
			};
		}
	}
}