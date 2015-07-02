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
			var status = "NORMAL";
			switch (message.Level)
			{
				case TraceMessageLevel.Warning:
					status = "WARNING";
					break;
				case TraceMessageLevel.Error:
					status = "ERROR";
					break;		
			}

			var formattedMessage = _formatter.FormatMessage("message", new
			{
				text = message.ToString("mlf"),
				status = status,
				errorDetails = message.Details ?? String.Empty
			});

			Console.WriteLine(formattedMessage);
		}		
	}
}