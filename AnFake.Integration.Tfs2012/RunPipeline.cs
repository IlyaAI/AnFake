using System;
using System.Activities;
using AnFake.Integration.Tfs2012.Pipeline;
using Microsoft.TeamFoundation.Build.Client;
using Microsoft.TeamFoundation.Build.Workflow.Services;
using Microsoft.TeamFoundation.Build.Workflow.Tracking;

namespace AnFake.Integration.Tfs2012
{
	[ActivityTracking(ActivityTrackingOption.ActivityOnly)]
	[BuildActivity(HostEnvironmentOption.All)]
	public sealed class RunPipeline : CodeActivity
	{
		[RequiredArgument]
		public InArgument<IBuildDetail> BuildDetail { get; set; }

		[RequiredArgument]
		public InArgument<string> Pipeline { get; set; }

		public InArgument<TimeSpan> SpinTime { get; set; }

		public InArgument<TimeSpan> Timeout { get; set; }

		public RunPipeline()
		{
			DisplayName = "Run Pipeline";
			SpinTime = new InArgument<TimeSpan>(TimeSpan.FromSeconds(15));
			Timeout = new InArgument<TimeSpan>(TimeSpan.MaxValue);
		}

		protected override void CacheMetadata(CodeActivityMetadata metadata)
		{
			base.CacheMetadata(metadata);

			metadata.RequireExtension<IBuildLoggingExtension>();
		}

		protected override void Execute(CodeActivityContext context)
		{
			var buildDetail = BuildDetail.Get(context);
			var pipelineDef = Pipeline.Get(context);
			var spinTime = SpinTime.Get(context);
			var timeout = Timeout.Get(context);

			var tracker = context
				.GetExtension<IBuildLoggingExtension>()
				.GetActivityTracking(context);

			using (var runner = new TfsPipelineRunner(buildDetail, tracker))
			{
				runner.Run(pipelineDef, spinTime, timeout);
			}
		}
	}
}