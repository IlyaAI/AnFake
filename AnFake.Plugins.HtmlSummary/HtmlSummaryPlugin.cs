using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;
using AnFake.Api;
using AnFake.Core;
using AnFake.Core.Integration;

namespace AnFake.Plugins.HtmlSummary
{
	internal sealed class HtmlSummaryPlugin
	{
		private static readonly string PluginName = typeof(HtmlSummaryPlugin).Namespace;
		private const string SummaryJs = "build.summary.js";
		private const string IndexHtml = "Index.html";
		
		private BuildSummary _summary;
		private FileSystemPath _tempPath;
		
		public HtmlSummaryPlugin()
		{
			Target.Finished += OnTargetFinished;

			MyBuild.Started += OnBuildStarted;			
			MyBuild.Finished += OnBuildFinished;
		}
		
		private void OnTargetFinished(object sender, Target.RunDetails details)
		{
			Summarize((Target)sender, details.ExecutedTargets);			
		}

		private void OnBuildStarted(object sender, MyBuild.RunDetails details)
		{
			_tempPath = "[Temp]".AsPath()/PluginName.MakeUnique();
			Folders.Clean(_tempPath);

			_summary = new BuildSummary
			{
				ComputerName = Environment.MachineName,
				StartTime = details.StartTime,
				FinishTime = details.StartTime,
				Status = details.Status
			};

			var vcs = Plugin.Find<IVersionControl>();
			if (vcs == null)
				return;

			var changesetId = vcs.CurrentChangesetId;
			_summary.ChangesetId = changesetId;
			_summary.ChangesetAuthor = vcs.GetChangeset(changesetId).Author;
		}

		private void OnBuildFinished(object sender, MyBuild.RunDetails details)
		{
			_summary.FinishTime = details.FinishTime;
			_summary.Status = details.Status;
			
			Expose();

			Folders.Delete(_tempPath);
		}

		private void Summarize(Target top, IEnumerable<Target> executedTargets)
		{
			var targetSummary = SummaryOf(top);

			foreach (var target in executedTargets)
			{
				targetSummary.Children.Add(SummaryOf(target));
			}

			_summary.Targets.Add(targetSummary);
		}		

		private void Expose()
		{
			Log.Text("-------- HTML Summary Plugin --------");
			Log.Text("Generating report...");

			using (var stream = new FileStream((_tempPath / SummaryJs).Full, FileMode.Create, FileAccess.Write))
			{
				var decl = Encoding.UTF8.GetBytes("var gSummary = ");
				stream.Write(decl, 0, decl.Length);

				new DataContractJsonSerializer(typeof(BuildSummary))
					.WriteObject(stream, _summary);
			}

			Zip.Unpack(
				"[AnFakePlugins]".AsPath() / PluginName + ".zip", 
				_tempPath,
				p => p.OverwriteMode = Zip.OverwriteMode.Overwrite);

			var logUri = BuildServer.ExposeArtifact(_tempPath.AsFolder(), ArtifactType.Logs);

			// TODO: append IndexHtml

			Log.Text("");
			Log.Text("Surprise! HtmlSummary plugin has generated a nice build report for you. Look here...");
			Log.TextFormat("[HTML Summary|{0}]", logUri);
		}

		private static TargetSummary SummaryOf(Target target)
		{
			var summary = new TargetSummary
			{
				Name = target.Name,				
				State = target.State,
				RunTimeMs = (long) target.RunTime.TotalMilliseconds,
				ErrorsCount = target.Messages.ErrorsCount,
				WarningsCount = target.Messages.WarningsCount,
				MessagesCount = target.Messages.SummariesCount,
			};

			foreach (var message in target.Messages)
			{
				summary.Messages.Add(message);
			}

			return summary;
		}		
	}
}