using System;
using System.Linq;
using System.Net;
using AnFake.Api;
using AnFake.Core;
using AnFake.Core.Integration;
using Antlr4.StringTemplate;

namespace AnFake.Plugins.HtmlSummary
{
	internal sealed class HtmlSummaryPlugin : PluginBase
	{
		private class ObjectModelAdaptor : Antlr4.StringTemplate.Misc.ObjectModelAdaptor
		{
			public override object GetProperty(Interpreter interpreter, TemplateFrame frame, object o, object property, string propertyName)
			{
				var ret = base.GetProperty(interpreter, frame, o, property, propertyName);

				var s = ret as String;
				return s != null ? WebUtility.HtmlEncode(s) : ret;
			}
		}
		
		private const string IndexHtml = "AnFake.summary.html";
		
		private BuildSummary _build;
		
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
							RunTime = t.RunTime.ToString("hh\\:mm\\:ss"),
							Messages = t.Messages
						})
				);

			_build.RequestedTargets.Add(topTargetSummary);			
		}

		private void OnBuildStarted(object sender, MyBuild.RunDetails details)
		{
			_build = new BuildSummary
			{
				AgentName = Environment.MachineName,
				Changeset = BuildServer.CurrentBuild.ChangesetHash,
				WorkingFolderUri = MyBuild.Current.Path.ToUnc().ToUri()
			};			
		}

		private void OnBuildFinished(object sender, MyBuild.RunDetails details)
		{
			_build.Status = details.Status.ToHumanReadable().ToUpperInvariant();
			_build.RunTime = (details.FinishTime - details.StartTime).ToString("hh\\:mm\\:ss");
			
			_build.LogFileUri =
				BuildServer.CanExposeArtifacts
					? BuildServer.ExposeArtifact(MyBuild.Current.LogFile, ".anfake")
					: MyBuild.Current.LogFile.Path.ToUnc().ToUri();

			var htmlFile = RenderHtml();

			var summaryUri =
				BuildServer.CanExposeArtifacts
					? BuildServer.ExposeArtifact(htmlFile, ".anfake")
					: htmlFile.Path.ToUnc().ToUri();

			Trace
				.Begin()
				.Summary().WithText("AnFake HTML build summary has generated. See link below.")
				.WithLink(summaryUri, IndexHtml)
				.End();
		}

		private FileItem RenderHtml()
		{
			var tmplGroup = new TemplateGroupFile("[AnFake]/AnFake.Plugins.HtmlSummary.stg".AsPath().Full, '`', '`');
			tmplGroup.RegisterModelAdaptor(typeof(Object), new ObjectModelAdaptor());

			var tmpl = tmplGroup.GetInstanceOf("main");
			tmpl.Add("build", _build);

			var htmlFile = IndexHtml.AsFile();
			Text.WriteTo(htmlFile, tmpl.Render());

			return htmlFile;
		}
    }
}