using System;
using System.Activities;
using System.Threading;
using AnFake.Integration.Tfs2012.Pipeline;
using Microsoft.TeamFoundation.Build.Client;
using Microsoft.TeamFoundation.Build.Workflow.Services;
using Microsoft.TeamFoundation.Build.Workflow.Tracking;
using Microsoft.TeamFoundation.VersionControl.Client;

namespace AnFake.Integration.Tfs2012
{
	[ActivityTracking(ActivityTrackingOption.ActivityOnly)]
	[BuildActivity(HostEnvironmentOption.All)]
	public sealed class RunPipeline : AsyncCodeActivity
	{
		private sealed class AsyncAdapter
		{
			private readonly CancellationTokenSource _cancellationSource = new CancellationTokenSource();
			private readonly Action<IBuildDetail, string, string, TimeSpan, TimeSpan, CancellationToken> _doRun;

			public AsyncAdapter(Action<IBuildDetail, string, string, TimeSpan, TimeSpan, CancellationToken> doRun)
			{
				_doRun = doRun;
			}

			public IAsyncResult BeginRun(
				IBuildDetail build, string activityInstanceId, 
				string pipelineDef, TimeSpan spinTime, TimeSpan timeout,
				AsyncCallback callback, object state)
			{
				return _doRun.BeginInvoke(
					build, activityInstanceId,
					pipelineDef, spinTime, timeout,
					_cancellationSource.Token,
					callback, state);
			}

			public void EndRun(IAsyncResult result)
			{
				try
				{
					_doRun.EndInvoke(result);
				}
				catch (OperationCanceledException)
				{
					// skip
				}				
			}

			public void Cancel()
			{
				_cancellationSource.Cancel();
			}
		}

		[RequiredArgument]
		public InArgument<IBuildDetail> BuildDetail { get; set; }

		[RequiredArgument]
		public InArgument<string> Pipeline { get; set; }

		public InArgument<TimeSpan> SpinTime { get; set; }

		public InArgument<TimeSpan> Timeout { get; set; }

		public InArgument<string> SourcesVersion { get; set; }

		public RunPipeline()
		{
			DisplayName = "Run Pipeline";
			SpinTime = new InArgument<TimeSpan>(TimeSpan.FromSeconds(15));
			Timeout = new InArgument<TimeSpan>(TimeSpan.MaxValue);
			SourcesVersion = new InArgument<string>();
		}

		protected override void CacheMetadata(CodeActivityMetadata metadata)
		{
			base.CacheMetadata(metadata);

			metadata.RequireExtension<IBuildLoggingExtension>();
		}

		protected override IAsyncResult BeginExecute(AsyncCodeActivityContext context, AsyncCallback callback, object state)
		{
			var buildDetail = BuildDetail.Get(context);
			var pipelineDef = Pipeline.Get(context);
			var spinTime = SpinTime.Get(context);
			var timeout = Timeout.Get(context);
			var version = SourcesVersion.Get(context);

			if (!String.IsNullOrEmpty(version))
			{
				// Validate specified version
				VersionSpec.ParseSingleSpec(version, ".");

				buildDetail.SourceGetVersion = version;
				buildDetail.Save();
			}

			var activityInstanceId = context
				.GetExtension<IBuildLoggingExtension>()
				.GetActivityTracking(context)
				.ActivityInstanceId;

			var asyncAdapter = new AsyncAdapter(DoRun);			
			context.UserState = asyncAdapter;

			return asyncAdapter.BeginRun(
				buildDetail,
				activityInstanceId,
				pipelineDef,
				spinTime,
				timeout,				
				callback, 
				state);
		}

		protected override void EndExecute(AsyncCodeActivityContext context, IAsyncResult result)
		{
			var asyncAdapter = (AsyncAdapter) context.UserState;
			asyncAdapter.EndRun(result);			
		}

		protected override void Cancel(AsyncCodeActivityContext context)
		{
			var asyncAdapter = (AsyncAdapter) context.UserState;
			if (asyncAdapter == null)
				return;
			
			asyncAdapter.Cancel();
		}

		private static void DoRun(
			IBuildDetail buildDetail, string activityInstanceId, 
			string pipelineDef, TimeSpan spinTime, TimeSpan timeout, 
			CancellationToken cancellationToken)
		{			
			using (var runner = new TfsPipelineRunner(buildDetail, activityInstanceId))
			{
				runner.Run(pipelineDef, spinTime, timeout, cancellationToken);
			}
		}
	}	
}