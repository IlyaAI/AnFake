using System;
using System.Activities;
using System.Activities.Statements;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization.Json;
using AnFake.Api;
using Microsoft.TeamFoundation.Build.Client;
using Microsoft.TeamFoundation.Build.Workflow.Activities;
using Microsoft.TeamFoundation.Build.Workflow.Tracking;

namespace AnFake.Integration.Tfs2012
{
	[ActivityTracking(ActivityTrackingOption.ActivityOnly)]
	[BuildActivity(HostEnvironmentOption.All)]
	public sealed class RestorePackages : Activity
	{
		private string EmbeddedNuGetPath { get; set; }

		[RequiredArgument]
		public InArgument<string> BuildDirectory { get; set; }

		[RequiredArgument]
		public InArgument<string> PackagesConfig { get; set; }

		public InArgument<string> Options { get; set; }

		public RestorePackages()
		{
			Options = new InArgument<string>();

			EmbeddedNuGetPath = GetEmbeddedNuGetPath();
			Implementation = CreateBody;
		}		

		private Activity CreateBody()
		{			
			var retCode = new Variable<int>();
			var dummy = new Variable<int>();
			var outBuffer = new Variable<ProcessOutputBuffer>(ctx => new ProcessOutputBuffer());
			
			var invokeProcess = new InvokeProcess
			{				
				FileName = new InArgument<string>(ctx => EmbeddedNuGetPath),
				Arguments = new InArgument<string>(
					ctx => new Args("-", " ")
						.Command("restore")
						.Param(PackagesConfig.Get(ctx))
						.Option("SolutionDirectory", BuildDirectory.Get(ctx))
						.Option("Source", GetSourceUrl(BuildDirectory.Get(ctx)))
						.Option("NonInteractive", true)
						.Other(Options.Get(ctx))
						.ToString()),
				WorkingDirectory = new InArgument<string>(ctx => BuildDirectory.Get(ctx)),
				Result = new OutArgument<int>(retCode),
				OutputDataReceived = RedirectToLogAndBuffer(outBuffer, dummy),
				ErrorDataReceived = RedirectToLogAndBuffer(outBuffer, dummy)
			};

			return new Sequence
			{
				Variables = { retCode, dummy, outBuffer },
				Activities =
				{
					LogInfo("Restoring NuGet packages..."),
					LogDebug("Using embedded NuGet: '{0}'.", EmbeddedNuGetPath),
					invokeProcess,
					CheckExitCode(retCode, outBuffer)
				}
			};
		}

		private static string GetEmbeddedNuGetPath()
		{
			var me = new FileInfo(Assembly.GetExecutingAssembly().Location);
			return me.Directory != null
				? Path.Combine(me.Directory.FullName, "NuGet.exe")
				: "NuGet.exe";
		}

		private static string GetSourceUrl(string buildDirectory)
		{
			var settingsPath = Path.Combine(buildDirectory, "AnFake.settings.json");
			if (!File.Exists(settingsPath))
				return null;

			using (var stream = new FileStream(settingsPath, FileMode.Open, FileAccess.Read))
			{
				var settings = (IDictionary<string, string>)new DataContractJsonSerializer(
					typeof(Dictionary<string, string>),
					new DataContractJsonSerializerSettings { UseSimpleDictionaryFormat = true })
						.ReadObject(stream);

				string sourceUrl;
				return settings.TryGetValue("NuGet.SourceUrl", out sourceUrl)
					? sourceUrl
					: null;
			}
		}

		private static ActivityAction<string> RedirectToLogAndBuffer(Variable<ProcessOutputBuffer> outBuffer, Variable<int> dummy)
		{
			var arg = new DelegateInArgument<string>();
			return new ActivityAction<string>
			{
				Argument = arg,
				Handler = new Sequence
				{
					Activities =
					{
						new WriteBuildMessage
						{
							Message = new InArgument<string>(arg),
							Importance = new InArgument<BuildMessageImportance>(BuildMessageImportance.High)
						},
						new Assign
						{
							To = new OutArgument<int>(dummy),
							Value = new InArgument<int>(ctx => outBuffer.Get(ctx).Append(arg.Get(ctx)))
						}
					}
				}				
			};
		}

		private static If CheckExitCode(Variable<int> retCode, Variable<ProcessOutputBuffer> outBuffer)
		{
			return new If(ctx => retCode.Get(ctx) != 0)
			{				
				Then = new Sequence
				{
					Activities =
					{
						new Throw
						{
							Exception = new InArgument<Exception>(
								ctx => new AnFakeBuildProcessException(
									"NuGet.exe has failed with exit code: {0}.\n" +
									"============= Process Output =============\n" +
									"{1}",
									retCode.Get(ctx),
									outBuffer.Get(ctx)))
						}
					}
				}
			};
		}

		private static WriteBuildMessage LogDebug(string format, params object[] args)
		{
			return new WriteBuildMessage
			{
				Message = new InArgument<string>(String.Format(format, args)),
				Importance = new InArgument<BuildMessageImportance>(BuildMessageImportance.Low)
			};
		}

		private static WriteBuildMessage LogInfo(string format, params object[] args)
		{
			return new WriteBuildMessage
			{
				Message = new InArgument<string>(String.Format(format, args)),
				Importance = new InArgument<BuildMessageImportance>(BuildMessageImportance.Normal)
			};
		}		
	}
}