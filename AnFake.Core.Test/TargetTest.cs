using System;
using System.Text;
using AnFake.Api;
using AnFake.Core.Exceptions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhino.Mocks;

namespace AnFake.Core.Test
{
	[TestClass]
	public class TargetTest
	{
		public ITracer PrevTracer;
		public ITracer Tracer;

		[TestInitialize]
		public void Initialize()
		{
			Tracer = MockRepository.GenerateMock<ITracer>();
			PrevTracer = Trace.Set(Tracer);
		}

		[TestCleanup]
		public void Cleanup()
		{
			Trace.Set(PrevTracer);
			Target.Reset();
		}

		[TestCategory("Functional")]
		[TestMethod]
		public void TargetRun_should_execute_in_correct_order()
		{
			// arrange
			var sb = new StringBuilder();

			"a".AsTarget().Do(() => sb.Append("a"));
			"b".AsTarget().Do(() => sb.Append("b"));
			"c".AsTarget().Do(() => sb.Append("c"));
			"d".AsTarget().Do(() => sb.Append("d"));					

			"b".AsTarget().DependsOn("a", "c", "d");
			"c".AsTarget().DependsOn("d");

			// act
			"b".AsTarget().Run();

			// assert
			Assert.AreEqual("adcb", sb.ToString());
		}

		[TestCategory("Functional")]
		[TestMethod]
		public void TargetRun_should_stop_execution_on_error()
		{
			// arrange
			var sb = new StringBuilder();

			"a".AsTarget().Do(() => sb.Append("a"));
			"b".AsTarget().Do(() => sb.Append("b"));
			"c".AsTarget().Do(() => sb.Append("c"));
			"d".AsTarget().Do(() => { throw new Exception("ERROR"); });

			"b".AsTarget().DependsOn("a", "c", "d");
			"c".AsTarget().DependsOn("d");

			// act
			try
			{
				"b".AsTarget().Run();
			}
			catch (TerminateTargetException)
			{
				// it's ok
			}			

			// assert
			Assert.AreEqual("a", sb.ToString());
			Tracer.AssertWasCalled(
				x => x.Write(
					Arg<TraceMessage>.Matches(y => y.Level == TraceMessageLevel.Error && y.Message == "ERROR")));
		}

		[TestCategory("Functional")]
		[TestMethod]		
		public void TargetRun_should_detect_cycling_dependencies()
		{
			// arrange
			"a".AsTarget().Do(() => { });
			"b".AsTarget().Do(() => { });
			"c".AsTarget().Do(() => { });
			"a".AsTarget().DependsOn("b");
			"b".AsTarget().DependsOn("c");
			"c".AsTarget().DependsOn("a");

			try
			{
				// act
				"a".AsTarget().Run();
			}
			catch (InvalidConfigurationException)
			{
				// assert				
			}
		}

		[TestCategory("Functional")]
		[TestMethod]
		public void TargetRun_should_execute_final_action_on_success()
		{
			// arrange
			var sb = new StringBuilder();

			"a".AsTarget()
				.Do(() => sb.Append("a"))
				.Finally(() => sb.Append("b"));
						
			// act
			"a".AsTarget().Run();

			// assert
			Assert.AreEqual("ab", sb.ToString());
		}

		[TestCategory("Functional")]
		[TestMethod]
		public void TargetRun_should_execute_final_action_on_failure()
		{
			// arrange
			var sb = new StringBuilder();

			"a".AsTarget()
				.Do(() => { throw new Exception("ERROR"); })
				.Finally(() => sb.Append("c"));

			// act
			try
			{
				"a".AsTarget().Run();
			}
			catch (TerminateTargetException)
			{
				// it's ok
			}			
			
			// assert
			Assert.AreEqual("c", sb.ToString());
			Tracer.AssertWasCalled(
				x => x.Write(
					Arg<TraceMessage>.Matches(y => y.Level == TraceMessageLevel.Error && y.Message == "ERROR")));
		}

		[TestCategory("Functional")]
		[TestMethod]
		public void TargetRun_should_execute_failure_action_on_failure()
		{
			// arrange
			var sb = new StringBuilder();

			"a".AsTarget()
				.Do(() => { throw new Exception("ERROR"); })
				.OnFailure(() => sb.Append("c"));

			// act
			try
			{
				"a".AsTarget().Run();
			}
			catch (TerminateTargetException)
			{
				// it's ok
			}			
			
			// assert
			Assert.AreEqual("c", sb.ToString());
			Tracer.AssertWasCalled(
				x => x.Write(
					Arg<TraceMessage>.Matches(y => y.Level == TraceMessageLevel.Error && y.Message == "ERROR")));
		}

		[TestCategory("Functional")]
		[TestMethod]
		public void TargetRun_should_do_nothing_if_target_succeeded()
		{
			// arrange
			var sb = new StringBuilder();
			"a".AsTarget().Do(() => sb.Append("a"));
			
			// act
			"a".AsTarget().Run();
			"a".AsTarget().Run();

			// assert
			Assert.AreEqual("a", sb.ToString());
		}

		[TestCategory("Functional")]
		[TestMethod]
		public void TargetRun_should_do_nothing_if_target_partially_succeeded()
		{
			// arrange
			var sb = new StringBuilder();
			"a".AsTarget().Do(() => sb.Append("a"));
			"b".AsTarget().Do(() =>
			{
				sb.Append("b");
				throw new Exception();
			}).SkipErrors();
			"a".AsTarget().DependsOn("b");

			// act
			"a".AsTarget().Run();
			"a".AsTarget().Run();

			// assert
			Assert.AreEqual("ba", sb.ToString());
		}

		[TestCategory("Functional")]
		[TestMethod]
		public void TargetRun_should_do_nothing_if_target_failed()
		{
			// arrange
			var sb = new StringBuilder();
			"a".AsTarget().Do(() =>
			{
				sb.Append("a");
				throw new Exception();
			}).SkipErrors();

			// act
			"a".AsTarget().Run();
			"a".AsTarget().Run();

			// assert
			Assert.AreEqual("a", sb.ToString());
		}
	}
}