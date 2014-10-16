using System;
using AnFake.Api;
using Common.Logging;
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
			var logFactory = MockRepository.GenerateMock<ILoggerFactoryAdapter>();
			var log = MockRepository.GenerateMock<ILog>();
			logFactory.Stub(x => x.GetLogger("AnFake.Core.Process")).Return(log);

			var tracer = MockRepository.GenerateMock<ITracer>();
			var result = MockRepository.GenerateMock<IToolExecutionResult>();
			tracer.Stub(x => x.StopTrackExternal()).Return(result);

			var prevFactory = LogManager.Adapter;
			LogManager.Adapter = logFactory;
			Tracer.Instance = tracer;
			try
			{
				// act
				Process.Run(def =>
				{
					def.FileName = "AnFake.Process.Test.exe".AsPath();
					def.Arguments = "--out \"stdout A\" --err \"stderr A\" --out \"stdout B\" --err \"stderr B\"";
				}).FailIfExitCodeNonZero("Unexpected exit code.");

				// assert
				log.AssertWasCalled(x => x.Debug("stdout A"));
				log.AssertWasCalled(x => x.Error("stderr A"));
				log.AssertWasCalled(x => x.Debug("stdout B"));
				log.AssertWasCalled(x => x.Error("stderr B"));
			}
			finally
			{
				Tracer.Instance = null;
				LogManager.Adapter = prevFactory;
			}
		}

		[TestCategory("Functional")]
		[TestMethod]
		public void ProcessRun_should_track_external_messages()
		{
			// arrange
			var tracer = new JsonFileTracer("process.log.jsx", false);
			
			Tracer.Instance = tracer;
			try
			{
				// act
				var result = Process.Run(def =>
				{
					def.FileName = "AnFake.Process.Test.exe".AsPath();
					def.Arguments = "--log 1 warning --log 2 error";
				});

				// assert
				Assert.AreEqual(1, result.ErrorsCount);
				Assert.AreEqual(1, result.WarningsCount);
				Assert.AreEqual(0, result.ExitCode);				
			}
			finally
			{
				Tracer.Instance = null;				
			}
		}

		[TestCategory("Functional")]
		[TestMethod]
		public void ProcessRun_should_return_exit_code()
		{
			// arrange
			var tracer = MockRepository.GenerateMock<ITracer>();
			var res = MockRepository.GenerateMock<IToolExecutionResult>();
			tracer.Stub(x => x.StopTrackExternal()).Return(res);

			Tracer.Instance = tracer;
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
				Tracer.Instance = null;
			}
		}

		[TestCategory("Functional")]
		[ExpectedException(typeof(TimeoutException))]
		[TestMethod]
		public void ProcessRun_should_terminate_by_timeout()
		{
			// arrange
			var tracer = MockRepository.GenerateMock<ITracer>();
			var res = MockRepository.GenerateMock<IToolExecutionResult>();
			tracer.Stub(x => x.StopTrackExternal()).Return(res);

			Tracer.Instance = tracer;
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
				Tracer.Instance = null;
			}
		}
	}
}