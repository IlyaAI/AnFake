using System;
using System.Activities;
using System.Diagnostics;
using System.IO;
using AnFake.Api;
using Microsoft.TeamFoundation.Build.Client;
using Microsoft.TeamFoundation.Build.Workflow;
using Microsoft.TeamFoundation.Build.Workflow.Activities;

namespace AnFake.Integration.Tfs2012
{
	[BuildActivity(HostEnvironmentOption.All)]
	public class InvokeAnFake : CodeActivity
	{
		[RequiredArgument]
		public InArgument<string> BuildDirectory { get; set; }

		[RequiredArgument]		
		public InArgument<string> Targets { get; set; }
				
		public InArgument<BuildVerbosity> Verbosity { get; set; }

		public InArgument<string> Parameters { get; set; }

		public InArgument<string> Script { get; set; }

		public InArgument<string> ToolPath { get; set; }

		public InArgument<TimeSpan> Timeout { get; set; }

		public InvokeAnFake()
		{
			Verbosity = new InArgument<BuildVerbosity>(BuildVerbosity.Normal);
			Parameters = new InArgument<string>("");
			Script = new InArgument<string>("build.fsx");
			ToolPath = new InArgument<string>(@".AnFake\Bin\AnFake.exe");
			Timeout = new InArgument<TimeSpan>(TimeSpan.MaxValue);
		}

		protected override void Execute(CodeActivityContext ctx)
		{
			//var buildDetail = context.GetExtension<IBuildDetail>();

			var buildDir = BuildDirectory.Get(ctx);

			var args = new Args("", "=")
				.Param(Script.Get(ctx))
				.Other(Targets.Get(ctx))
				.Option("MsBuild.Verbosity", Verbosity.Get(ctx))
				.Other(Parameters.Get(ctx));

			var process = new Process
			{
				StartInfo =
				{
					FileName = Path.Combine(buildDir, @".AnFake\Bin\AnFake.exe"),
					WorkingDirectory = buildDir,
					Arguments = args.ToString()
				}
			};

			var tracer = new JsonFileTracer(Path.Combine(buildDir, "build.log.jsx"), true);
			var collector = new TraceMessageCollector();
			tracer.MessageReceived += collector.OnMessage;
			tracer.StartTrackExternal();			
			try
			{
				process.Start();

				var timeout = Timeout.Get(ctx);
				if (timeout == TimeSpan.MaxValue)
				{
					process.WaitForExit();
				}
				else if (!process.WaitForExit((int)timeout.TotalMilliseconds))
				{
					process.Kill();
					throw new TimeoutException(String.Format("AnFake isn't completed in specified time. Timeout: {0}", timeout));
				}
			}
			finally
			{
				tracer.StopTrackExternal();
			}

			foreach (var message in collector)
			{
				switch (message.Level)
				{
					case TraceMessageLevel.Debug:
					case TraceMessageLevel.Info:
						ctx.TrackBuildMessage(message.Message);
						break;

					case TraceMessageLevel.Warning:
						ctx.TrackBuildWarning(message.Message);
						break;

					case TraceMessageLevel.Error:
						ctx.TrackBuildError(message.Message);
						break;
				}
			}
		}
	}
}