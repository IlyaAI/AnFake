using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AnFake.Api;
using AnFake.Core;
using AnFake.Core.Exceptions;
using AnFake.Logging;
using AnFake.Plugins.Tfs2012;

namespace AnFake.Integration.TfWorkspacer
{
	internal class Workspacer
	{
		private class RunOptions
		{
			public readonly IDictionary<string, string> Properties = new Dictionary<string, string>();			
			public Verbosity Verbosity = Verbosity.Normal;
			public string BuildPath;			
		}

		[STAThread]
		public static int Main(string[] args)
		{
			if (args.Length == 0)
			{
				Console.WriteLine("Usage: AnFake.Integration.TfWorkspacer.exe <server-path> <local-path> Tfs.Uri=<tfs-uri> [Version=<version-spec>] [Comment=<workspace-comment>]");				
				return 0;
			}

			AnFakeException.StackTraceMode = StackTraceMode.Full;
			
			var options = new RunOptions
			{
				BuildPath = Directory.GetCurrentDirectory()
			};
			try
			{				
				ParseCommandLine(args, options);

				ConfigureLogger(options);
				ConfigureTracer(options);
			}			
			catch (Exception e)
			{
				ConsoleLogError("AnFake failed in initiation phase. See details below.", e);
				return (int) MyBuild.Status.Unknown;
			}

			return Run(options);
		}

		private static void ParseCommandLine(IEnumerable<string> args, RunOptions options)
		{
			var propIndex = 1;

			foreach (var arg in args)
			{
				if (arg.Contains("="))
				{
					var index = arg.IndexOf("=", StringComparison.InvariantCulture);

					options.Properties[arg.Substring(0, index).Trim()] = arg.Substring(index + 1).Trim();
					continue;
				}

				options.Properties.Add("__" + propIndex++, arg.Trim());				
			}			
		}

		private static void ConfigureLogger(RunOptions options)
		{
			options.Verbosity = Verbosity.Normal;
			
			string value;
			if (options.Properties.TryGetValue("Verbosity", out value))
			{
				if (!Enum.TryParse(value, true, out options.Verbosity))
					throw new ArgumentException(
						String.Format(
							"Unrecognized value '{0}'. Verbosity = {{{1}}}",
							value,
							String.Join("|", Enum.GetNames(typeof (Verbosity)))));

				options.Properties.Remove("Verbosity");
			}

			Log.Set(new Log4NetLogger(null, options.Verbosity, Int32.MaxValue));
		}

		private static void ConfigureTracer(RunOptions options)
		{
			Trace.Set(new BypassTracer
			{
				Threshold = options.Verbosity.AsTraceLevelThreshold()
			});
		}

		private static int Run(RunOptions options)
		{
			try
			{
				var buildPath = options.BuildPath.AsPath();
				
				FileSystemPath.Base = buildPath;
				
				Trace.InfoFormat("BuildPath    : {0}", buildPath);				
				Trace.InfoFormat("Verbosity    : {0}", options.Verbosity);				
				Trace.InfoFormat("Properties   :\n  {0}", 
					String.Join("\n  ", options.Properties.Select(x => x.Key + " = " + x.Value)));

				MyBuild.Initialize(
						buildPath,
						null,
						null,
						options.Verbosity,
						new [] {"GetSources"},
						options.Properties);

				Trace.InfoFormat("AnFakeVersion: {0}", MyBuild.Current.AnFakeVersion);

				Tfs.PlugIn();
				"GetSources".AsTarget().Do(GetSources);
				
				Trace.Info("Configuring plugins...");
				Plugin.Configure();
				
				var status = MyBuild.Run();				
				MyBuild.Finalise();

				return (status - MyBuild.Status.Succeeded);
			}
			catch (Exception e)
			{				
				Log.Error(AnFakeException.Wrap(e));
				return (int) MyBuild.Status.Unknown;
			}
		}

		private static void GetSources()
		{			
			var serverPath = MyBuild.GetProp("__1").AsServerPath();
			var localPath = MyBuild.GetProp("__2").AsPath();
			var versionSpec = MyBuild.GetProp("Version", "T");
			
			Trace.InfoFormat("Checking workspace for folder '{0}'...", localPath.Full);
			
			var wsName = TfsWorkspace.Find(localPath);
			if (wsName != null)
			{
				Trace.InfoFormat("...found. Workspace '{0}' will be updated.", wsName);
				TfsWorkspace.Get(localPath, p => p.VersionSpec = versionSpec);
			}
			else
			{
				Trace.Info("...not found. New workspace will be created.");
				
				wsName = String.Format("AnFake-{0:N}", Guid.NewGuid());
				var wsComment = MyBuild.GetProp("Comment", "");

				TfsWorkspace.Checkout(
					serverPath, localPath, wsName,
					p =>
					{
						p.VersionSpec = versionSpec;
						p.WorkspaceComment = wsComment;
						//
						// W/A. TeamCity resolves artifacts before first build runner starts, 
						// so checkout directory might be non-empty when we start downloading sources.
						//
						p.AllowCheckoutToNonEmptyFolder = true;
					});
			}
		}

		private static void ConsoleLogError(string message, Exception exception)
		{
			var prevColor = Console.ForegroundColor;

			Console.ForegroundColor = ConsoleColor.Red;
			Console.WriteLine(message);
			Console.WriteLine(exception);
			Console.ForegroundColor = prevColor;
		}		
	}
}