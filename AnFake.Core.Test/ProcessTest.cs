using System;
using System.Text;
using AnFake.Api;
using AnFake.Core.Exceptions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhino.Mocks;

namespace AnFake.Core.Test
{
	[DeploymentItem("AnFake.Process.Test.exe")]
	[TestClass]
	public class ProcessTest
	{
		[TestCategory("Functional")]
		[TestMethod]
		public void ProcessRun_should_redirect_to_log()
		{
			// arrange
			var logger = MockRepository.GenerateMock<ILogger>();
			var tracer = MockRepository.GenerateMock<ITracer>();
			
			var prevLogger = Log.Set(logger);
			var prevTracer = Trace.Set(tracer);
			try
			{
				// act
				Process.Run(def =>
				{
					def.FileName = "AnFake.Process.Test.exe".AsPath();
					def.Arguments = "--out \"stdout A\" --err \"stderr A\" --out \"stdout B\" --err \"stderr B\"";
				}).FailIfExitCodeNonZero("Unexpected exit code.");

				// assert
				logger.AssertWasCalled(x => x.Write(LogMessageLevel.Debug, "stdout A"));
				logger.AssertWasCalled(x => x.Write(LogMessageLevel.Error, "stderr A"));
				logger.AssertWasCalled(x => x.Write(LogMessageLevel.Debug, "stdout B"));
				logger.AssertWasCalled(x => x.Write(LogMessageLevel.Error, "stderr B"));
			}
			finally
			{
				Trace.Set(prevTracer);
				Log.Set(prevLogger);				
			}
		}

		[TestCategory("Functional")]
		[TestMethod]
		public void ProcessRun_should_track_external_messages()
		{
			// arrange
			var tracer = new JsonFileTracer("process.log.jsx", false);
			
			var prevTracer = Trace.Set(tracer);
			try
			{
				// act
				var result = Process.Run(def =>
				{
					def.FileName = "AnFake.Process.Test.exe".AsPath();
					def.Arguments = "--log 3 warning --log 4 error";
					def.TrackExternalMessages = true;
				});

				// assert
				Assert.AreEqual(1, result.ErrorsCount);
				Assert.AreEqual(1, result.WarningsCount);
				Assert.AreEqual(0, result.ExitCode);				
			}
			finally
			{
				Trace.Set(prevTracer);
			}
		}

		[TestCategory("Functional")]
		[TestMethod]
		public void ProcessRun_should_return_exit_code()
		{
			// arrange
			var tracer = MockRepository.GenerateMock<ITracer>();
			
			var prevTracer = Trace.Set(tracer);
			try
			{
				// act
				var result = Process.Run(def =>
				{
					def.FileName = "AnFake.Process.Test.exe".AsPath();
					def.Arguments = "--exit 1";
				});

				// assert
				Assert.AreEqual(1, result.ExitCode);
			}
			finally
			{
				Trace.Set(prevTracer);
			}
		}

		[TestCategory("Functional")]
		[ExpectedException(typeof(TimeoutException))]
		[TestMethod]
		public void ProcessRun_should_terminate_by_timeout()
		{
			// arrange
			var tracer = MockRepository.GenerateMock<ITracer>();
			
			var prevTracer = Trace.Set(tracer);
			try
			{
				// act
				Process.Run(def =>
				{
					def.FileName = "AnFake.Process.Test.exe".AsPath();
					def.Arguments = "--wait 100";
					def.Timeout = TimeSpan.FromSeconds(1);
				});

				// assert				
			}
			finally
			{
				Trace.Set(prevTracer);
			}
		}

		[TestCategory("Functional")]
		[TestMethod]
		public void ProcessRun_should_throw_exception_if_file_not_found()
		{
			// arrange
			
			try
			{
				// act
				Process.Run(def => def.FileName = "noname.exe".AsPath());

				Assert.Fail("AnFakeException expected.");
			}
			catch (AnFakeException e)
			{
				// assert
				Assert.IsTrue(e.Message.Contains("noname.exe"));
			}			
		}

		[TestCategory("Functional")]
		[TestMethod]
		public void ProcessRun_should_ignore_utf8_bom()
		{
			// arrange
			var msg = Console.OutputEncoding.GetString(new byte[] {0xEF, 0xBB, 0xBF});

			var logger = MockRepository.GenerateMock<ILogger>();
			var tracer = MockRepository.GenerateMock<ITracer>();

			var prevLogger = Log.Set(logger);
			var prevTracer = Trace.Set(tracer);
			try
			{
				// act
				Process.Run(def =>
				{
					def.FileName = "AnFake.Process.Test.exe".AsPath();
					def.Arguments = "--out-utf8 \"\" --err-utf8 \"\"";
				}).FailIfExitCodeNonZero("Unexpected exit code.");

				// assert
				logger.AssertWasNotCalled(x => x.Write(LogMessageLevel.Debug, msg));
				logger.AssertWasNotCalled(x => x.Write(LogMessageLevel.Error, msg));
			}
			finally
			{
				Trace.Set(prevTracer);
				Log.Set(prevLogger);
			}
		}

		[TestCategory("Functional")]
		[TestMethod]
		public void ProcessRun_should_process_utf8_output()
		{
			// arrange
			var logger = MockRepository.GenerateMock<ILogger>();
			var tracer = MockRepository.GenerateMock<ITracer>();

			var prevLogger = Log.Set(logger);
			var prevTracer = Trace.Set(tracer);
			try
			{
				// act
				Process.Run(def =>
				{
					def.FileName = "AnFake.Process.Test.exe".AsPath();
					def.Arguments = "--out-utf8 \"по-русски\" --err-utf8 \"en español\"";
				}).FailIfExitCodeNonZero("Unexpected exit code.");

				// assert
				logger.AssertWasCalled(x => x.Write(LogMessageLevel.Debug, "по-русски"));
				logger.AssertWasCalled(x => x.Write(LogMessageLevel.Error, "en español"));
			}
			finally
			{
				Trace.Set(prevTracer);
				Log.Set(prevLogger);
			}
		}				
	}
}