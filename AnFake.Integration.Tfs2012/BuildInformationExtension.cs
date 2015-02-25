using System;
using AnFake.Api;
using Microsoft.TeamFoundation.Build.Client;

namespace AnFake.Integration.Tfs2012
{
	public static class BuildInformationExtension
	{
		public static void TraceMessage(this IBuildInformation info, TraceMessage message)
		{
			switch (message.Level)
			{
				case TraceMessageLevel.Debug:
					info.AddBuildMessage(message.ToString("mfd"), BuildMessageImportance.Low, DateTime.Now);
					break;

				case TraceMessageLevel.Info:
					info.AddBuildMessage(message.ToString("mfd"), BuildMessageImportance.Normal, DateTime.Now);
					break;

				case TraceMessageLevel.Summary:
					info.AddBuildMessage(message.ToString("mfd"), BuildMessageImportance.High, DateTime.Now);
					break;

				case TraceMessageLevel.Warning:
					info.AddBuildWarning(FormatMessage(message), DateTime.Now);
					break;

				case TraceMessageLevel.Error:
					info.AddBuildError(FormatMessage(message), DateTime.Now);
					break;
			}
		}

		private static string FormatMessage(TraceMessage message)
		{
			return new TfsMessageBuilder()
				.Append(message.ToString("mfd"))
				.AppendLinks(message.Links, "\n")
				.ToString();
		}
	}
}