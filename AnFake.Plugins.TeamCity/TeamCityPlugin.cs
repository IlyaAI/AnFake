using System;
using AnFake.Api;
using AnFake.Core;
using JetBrains.TeamCity.ServiceMessages.Write;

namespace AnFake.Plugins.TeamCity
{
	internal sealed class TeamCityPlugin : /*Core.Integration.IBuildServer,*/ IDisposable
	{
		private readonly ServiceMessageFormatter _formatter;

		public TeamCityPlugin()
		{
			_formatter = new ServiceMessageFormatter();

			Log.DisableConsoleEcho();
			Trace.MessageReceived += OnTraceMessage;			
		}

		// IDisposable members

		public void Dispose()
		{
			Trace.MessageReceived -= OnTraceMessage;
			
			if (Disposed != null)
			{
				SafeOp.Try(Disposed);
			}
		}

		public event Action Disposed;

		// Event Handlers

		private void OnTraceMessage(object sender, TraceMessage message)
		{
			var formattedMessage = (string)null;
			switch (message.Level)
			{
				case TraceMessageLevel.Warning:
					formattedMessage = _formatter.FormatMessage("message", new
					{
						text = message.ToString("mlf"),
						status = "WARNING"
					});
					break;

				case TraceMessageLevel.Error:
					formattedMessage = _formatter.FormatMessage("message", new
					{
						text = message.ToString("mlf"),
						status = "ERROR",
						errorDetails = message.Details ?? String.Empty
					});
					break;		

				default:
					formattedMessage = message.ToString("mlfd");
					break;
			}
			
			Console.WriteLine(formattedMessage);
		}		
	}
}