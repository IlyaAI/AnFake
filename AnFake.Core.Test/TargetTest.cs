using System;
using System.Text;
using AnFake.Api;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhino.Mocks;

namespace AnFake.Core.Test
{
	[TestClass]
	public class TargetTest
	{		
		[TestInitialize]
		public void Initialize()
		{
			Tracer.Instance = MockRepository.GenerateMock<ITracer>();
		}

		[TestCleanup]
		public void Cleanup()
		{
			Tracer.Instance = null;
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
			"b".AsTarget().Run();

			// assert
			Assert.AreEqual("a", sb.ToString());
			Tracer.Instance.AssertWasCalled(
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
			catch (InvalidOperationException)
			{
				// assert				
			}
		}

		[TestCategory("Functional")]
		[TestMethod]
		public void TargetRun_should_execute_final_target_on_success()
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
		public void TargetRun_should_execute_final_target_on_failure()
		{
			// arrange
			var sb = new StringBuilder();

			"a".AsTarget()
				.Do(() => { throw new Exception("ERROR"); })
				.Finally(() => sb.Append("c"));

			// act
			"a".AsTarget().Run();
			
			// assert
			Assert.AreEqual("c", sb.ToString());
			Tracer.Instance.AssertWasCalled(
				x => x.Write(
					Arg<TraceMessage>.Matches(y => y.Level == TraceMessageLevel.Error && y.Message == "ERROR")));
		}

		[TestCategory("Functional")]
		[TestMethod]
		public void TargetRun_should_execute_failure_target_on_failure()
		{
			// arrange
			var sb = new StringBuilder();

			"a".AsTarget()
				.Do(() => { throw new Exception("ERROR"); })
				.OnFailure(() => sb.Append("c"));

			// act
			"a".AsTarget().Run();
			
			// assert
			Assert.AreEqual("c", sb.ToString());
			Tracer.Instance.AssertWasCalled(
				x => x.Write(
					Arg<TraceMessage>.Matches(y => y.Level == TraceMessageLevel.Error && y.Message == "ERROR")));
		}		
	}
}