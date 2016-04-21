using System;
using AnFake.Api;
using AnFake.Core;
using AnFake.Core.Exceptions;
using ScriptCs;
using ScriptCs.Contracts;
using ScriptCs.Engine.Roslyn;

namespace AnFake.Scripting
{
	internal class CSharpEvaluator : IScriptEvaluator
	{
		private sealed class MyScriptHostFactory : IScriptHostFactory
		{
			public IScriptHost CreateScriptHost(IScriptPackManager scriptPackManager, string[] scriptArgs)
			{
				return new CSharpScriptHost(scriptPackManager, new ScriptEnvironment(scriptArgs));
			}
		}

		private sealed class MyLogProvider : ILogProvider
		{
			private sealed class NestedBlock : IDisposable
			{
				private readonly string _blockName;

				public NestedBlock(string blockName)
				{
					_blockName = blockName;

					Log.InfoFormat("CSC BEGIN: {0}", _blockName);
				}

				public void Dispose()
				{					
					Log.InfoFormat("CSC END: {0}", _blockName);
				}
			}

			public Logger GetLogger(string name)
			{
				return Logger;
			}

			public IDisposable OpenNestedContext(string message)
			{
				return new NestedBlock(message);
			}

			public IDisposable OpenMappedContext(string key, string value)
			{
				return new NestedBlock(String.Format("{0} = {1}", key, value));
			}

			private static bool Logger(LogLevel logLevel, Func<string> messageFunc, Exception exception, params object[] formatParameters)
			{
				LogMessageLevel myLevel;
				switch (logLevel)
				{
					case LogLevel.Trace:
					case LogLevel.Debug:
						if (Log.Threshold > LogMessageLevel.Debug)
							return false;
						myLevel = LogMessageLevel.Debug;
						break;

					case LogLevel.Info:
						if (Log.Threshold > LogMessageLevel.Info)
							return false;
						myLevel = LogMessageLevel.Info;
						break;

					case LogLevel.Warn:
						if (Log.Threshold > LogMessageLevel.Warning)
							return false;
						myLevel = LogMessageLevel.Warning;
						break;

					case LogLevel.Error:
					case LogLevel.Fatal:
						if (Log.Threshold > LogMessageLevel.Error)
							return false;
						myLevel = LogMessageLevel.Error;
						break;

					default:
						return false;
				}

				if (messageFunc != null)
				{
					Log.Message(myLevel, "CSC: " + String.Format(messageFunc(), formatParameters));
				}
				if (exception != null)
				{
					Log.ErrorFormat("CSC: {0}", exception);
				}
				
				return true;
			}
		}

		public FileSystemPath GetBasePath(FileItem script)
		{
			return script.Folder;
		}

		public void Evaluate(FileItem script, bool debug)
		{
			AnFakeException.ScriptSource = new ScriptSourceInfo(script.Name);

			var logProvider = new MyLogProvider();
			var fs = new ScriptCs.FileSystem();
			var lineProcessors = new ILineProcessor[]
			{
				new ReferenceLineProcessor(fs),
				new LoadLineProcessor(fs),
				new UsingLineProcessor()
			};
			var preProcessor = new FilePreProcessor(fs, logProvider, lineProcessors);
			var hostFactory = new MyScriptHostFactory();
			var engine = new CSharpScriptInMemoryEngine(hostFactory, logProvider);
			
			var executor = new ScriptExecutor(fs, preProcessor, engine, logProvider);
			
			executor.Initialize(new string[0], new IScriptPack[0]);			
			executor.Execute(script.Path.Full);
		}
	}
}