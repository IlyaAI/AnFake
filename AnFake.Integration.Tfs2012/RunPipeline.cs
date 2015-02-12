using System;
using System.Activities;
using System.Activities.Statements;
using AnFake.Integration.Tfs2012.Pipeline;
using AnFake.Api.Pipeline;
using Microsoft.TeamFoundation.Build.Client;
using Microsoft.TeamFoundation.Build.Workflow.Activities;
using Microsoft.TeamFoundation.Build.Workflow.Services;
using Microsoft.TeamFoundation.Build.Workflow.Tracking;

namespace AnFake.Integration.Tfs2012
{
	[ActivityTracking(ActivityTrackingOption.ActivityOnly)]
	[BuildActivity(HostEnvironmentOption.All)]
	public sealed class RunPipeline : Activity
	{
		[RequiredArgument]
		public InArgument<IBuildDetail> BuildDetail { get; set; }

		[RequiredArgument]
		public InArgument<string> Pipeline { get; set; }

		public RunPipeline()
		{
			DisplayName = "Run Pipeline";
			Implementation = CreateBody;
		}

		protected override void CacheMetadata(ActivityMetadata metadata)
		{
			base.CacheMetadata(metadata);

			metadata.RequireExtension<IBuildLoggingExtension>();
		}

		private Activity CreateBody()
		{
			var pipeline =
				new Variable<TfsPipeline>(ctx => new TfsPipeline(BuildDetail.Get(ctx), ctx.GetExtension<IBuildLoggingExtension>().GetActivityTracking(ctx)));
			
			var initialStep = 
				new Variable<PipelineStep>(ctx => PipelineCompiler.Compile(Pipeline.Get(ctx)));

			var status = new Variable<PipelineStepStatus>(ctx => PipelineStepStatus.InProgress);

			var runPipeline = new While(ctx => status.Get(ctx) == PipelineStepStatus.InProgress)
			{				
				Body = new Sequence
				{
					Activities =
					{
						new Assign<PipelineStepStatus>
						{
							Value = new InArgument<PipelineStepStatus>(ctx => initialStep.Get(ctx).Step(pipeline.Get(ctx))),
							To = new OutArgument<PipelineStepStatus>(status)
						},
						new If(ctx => status.Get(ctx) == PipelineStepStatus.InProgress)
						{
							Then = new Delay
							{
								Duration = new InArgument<TimeSpan>(TimeSpan.FromSeconds(30))
							}
						}
					}
				}
			};

			var setBuildStatus = new Switch<PipelineStepStatus>(ctx => status.Get(ctx))
			{
				Cases =
				{
					{
						PipelineStepStatus.Succeeded,
						new SetBuildProperties
						{
							PropertiesToSet = BuildUpdate.Status,
							Status = new InArgument<BuildStatus>(BuildStatus.Succeeded)
						}
					},
					{
						PipelineStepStatus.PartiallySucceeded,
						new SetBuildProperties
						{
							PropertiesToSet = BuildUpdate.Status,
							Status = new InArgument<BuildStatus>(BuildStatus.PartiallySucceeded)
						}
					}
				},
				Default =
					new SetBuildProperties
					{
						PropertiesToSet = BuildUpdate.Status,
						Status = new InArgument<BuildStatus>(BuildStatus.Failed)
					}
			};

			return new Sequence
			{
				Variables = {pipeline, initialStep, status},
				Activities =
				{
					new InvokeMethod
					{						
						MethodName = "LogSummary",
						Parameters = {new InArgument<string>(ctx => Pipeline.Get(ctx))},
						TargetObject = new InArgument<TfsPipeline>(pipeline)
					},
					new InvokeMethod
					{						
						MethodName = "LogSummary",
						Parameters = {new InArgument<string>("")},
						TargetObject = new InArgument<TfsPipeline>(pipeline)
					},
					
					runPipeline, 
					setBuildStatus,
					
					new InvokeMethod
					{						
						MethodName = "LogSummary",
						Parameters = {new InArgument<string>(new string('=', 48))},
						TargetObject = new InArgument<TfsPipeline>(pipeline)
					},
					new InvokeMethod
					{						
						MethodName = "LogSummary",
						Parameters = {new InArgument<string>(ctx => String.Format("Pipeline {0}", status.Get(ctx).ToUpperHumanReadable()))},
						TargetObject = new InArgument<TfsPipeline>(pipeline)
					}
				}
			};
		}
	}
}