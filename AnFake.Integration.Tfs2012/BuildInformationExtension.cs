using System;
using AnFake.Api;
using Microsoft.TeamFoundation.Build.Client;

namespace AnFake.Integration.Tfs2012
{
	public static class BuildInformationExtension
	{
		public static void TraceMessage(this IBuildInformation info, TraceMessage message, bool embedLinks = false)
		{
			switch (message.Level)
			{
				case TraceMessageLevel.Debug:
					info.AddBuildMessage(FormatMessage(message, embedLinks), BuildMessageImportance.Low, DateTime.Now);
					break;

				case TraceMessageLevel.Info:
					info.AddBuildMessage(FormatMessage(message, embedLinks), BuildMessageImportance.Normal, DateTime.Now);
					break;

				case TraceMessageLevel.Summary:
					info.AddBuildMessage(FormatMessage(message, embedLinks), BuildMessageImportance.High, DateTime.Now);
					break;

				case TraceMessageLevel.Warning:
					info.AddBuildWarning(FormatMessage(message, embedLinks), DateTime.Now);
					break;

				case TraceMessageLevel.Error:
					info.AddBuildError(FormatMessage(message, embedLinks), DateTime.Now);
					break;
			}
		}

		private static string FormatMessage(TraceMessage message, bool embedLinks)
		{
			var builder = new TfsMessageBuilder();

			builder.Append(message.ToString("mfd"));

			if (embedLinks)
			{
				builder.EmbedLinks(message.Links);
			}
			else
			{
				builder.AppendLinks(message.Links, "\n");
			}
				
			return builder.ToString();
		}
	}
}