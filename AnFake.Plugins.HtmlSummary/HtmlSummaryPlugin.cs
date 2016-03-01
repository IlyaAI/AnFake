using System;
using System.Linq;
using AnFake.Api;
using AnFake.Core;
using AnFake.Core.Integration;
using Antlr4.StringTemplate;

namespace AnFake.Plugins.HtmlSummary
{
	internal sealed class HtmlSummaryPlugin : PluginBase
	{
		private static readonly string PluginName = typeof(HtmlSummaryPlugin).Namespace;		
		private const string IndexHtml = "Index.html";
		
		private BuildSummary _summary;
		private FileSystemPath _tempPath;
		
		public HtmlSummaryPlugin()
		{
			Target.TopFinished += OnTopTargetFinished;

			MyBuild.Started += OnBuildStarted;
			MyBuild.Finished += OnBuildFinished;
		}

		public override void Dispose()
		{
			Target.TopFinished -= OnTopTargetFinished;

			MyBuild.Started -= OnBuildStarted;
			MyBuild.Finished -= OnBuildFinished;

			base.Dispose();
		}

		private void OnTopTargetFinished(object sender, Target.RunDetails details)
		{
			var topTarget = (Target)sender;
			var topTargetSummary = new BuildSummary.RequestedTarget
			{
				Name = topTarget.Name,
				State = topTarget.State.ToHumanReadable().ToUpperInvariant()
			};

			topTargetSummary.ExecutedTargets
				.AddRange(
					details.ExecutedTargets.Select(
						t => new BuildSummary.ExecutedTarget
						{
							Name = t.Name,
							State = t.State.ToHumanReadable().ToUpperInvariant(),
							RunTime = t.RunTime.ToString("hh:mm:ss"),
							Messages = t.Messages
						})
				);

			_summary.RequestedTargets.Add(topTargetSummary);			
		}

		private void OnBuildStarted(object sender, MyBuild.RunDetails details)
		{
			_tempPath = "[Temp]".AsPath()/PluginName.MakeUnique();
			Folders.Clean(_tempPath);

			_summary = new BuildSummary
			{
				AgentName = Environment.MachineName,
				Changeset = BuildServer.CurrentChangesetHash,
				WorkingFolder = MyBuild.Current.Path.ToUnc().Spec
			};			
		}

		private void OnBuildFinished(object sender, MyBuild.RunDetails details)
		{
			_summary.Status = details.Status.ToHumanReadable().ToUpperInvariant();
			_summary.RunTime = (details.FinishTime - details.StartTime).ToString("hh:mm:ss");

			_summary.LogFile = "build.log";
			Files.Copy(MyBuild.Current.LogFile.Path, _tempPath / _summary.LogFile, true);
			
			RenderHtml();

			//Folders.Delete(_tempPath);
		}				

		private void RenderHtml()
		{
			Trace.Info("-------- HTML Summary Plugin --------");
			Trace.Info("Generating report...");

			var tmplGroup = new TemplateGroupFile("[AnFake]/AnFake.Plugins.HtmlSummary.stg".AsPath().Full, '`', '`');

			var tmpl = tmplGroup.GetInstanceOf("main");
			tmpl.Add("summary", _summary);

			Text.WriteTo((_tempPath / IndexHtml).AsFile(), tmpl.Render());

			/*var logUri = BuildServer.ExposeArtifact(_tempPath.AsFolder(), ArtifactType.Logs);

			// TODO: append IndexHtml

			Log.Text("");
			Log.Text("Surprise! HtmlSummary plugin has generated a nice build report for you. Look here...");
			Log.TextFormat("[HTML Summary|{0}]", logUri);*/
		}
    }
}