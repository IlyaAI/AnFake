using System;
using System.Text;
using AnFake.Api;
using AnFake.Core.Exceptions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

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
			Tracer = new BypassTracer();
			PrevTracer = Trace.Set(Tracer);
		}

		[TestCleanup]
		public void Cleanup()
		{
			Trace.Set(PrevTracer);
			Target.Finalise();
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
			"d".AsTarget().Do(() =>
			{
				sb.Append("d");
				throw new Exception("ERROR");
			});

			"b".AsTarget().DependsOn("a", "c", "d");
			"c".AsTarget().DependsOn("d");

			// act
			SafeOp.Try("b".AsTarget().Run);		

			// assert
			Assert.AreEqual("ad", sb.ToString());			
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
				.Do(() =>
				{
					sb.Append("a");
					throw new Exception("ERROR");
				})
				.Finally(() => sb.Append("c"));

			// act
			SafeOp.Try("a".AsTarget().Run);			
			
			// assert
			Assert.AreEqual("ac", sb.ToString());			
		}

		[TestCategory("Functional")]
		[TestMethod]
		public void TargetRun_should_execute_failure_action_on_failure()
		{
			// arrange
			var sb = new StringBuilder();

			"a".AsTarget()
				.Do(() =>
				{
					sb.Append("a");
					throw new Exception("ERROR"); 
				})
				.OnFailure(() => sb.Append("c"));

			// act
			SafeOp.Try("a".AsTarget().Run);
			
			// assert
			Assert.AreEqual("ac", sb.ToString());			
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

		[TestCategory("Functional")]
		[TestMethod]
		public void TargetRun_should_eval_state_as_succeeded_if_all_dependents_succeeded()
		{
			// arrange			
			"a".AsTarget().Do(() => { });
			var b = "b".AsTarget()
				.Do(() => { })
				.DependsOn("a");

			// act
			b.Run();

			// assert
			Assert.AreEqual(TargetState.Succeeded, b.State);
		}

		[TestCategory("Functional")]
		[TestMethod]
		public void TargetRun_should_eval_state_as_failed_if_any_dependent_failed()
		{
			// arrange			
			"a".AsTarget().Do(() => { throw new TerminateTargetException(); });
			var b = "b".AsTarget()
				.Do(() => { })
				.DependsOn("a");

			// act
			SafeOp.Try(b.Run);		

			// assert
			Assert.AreEqual(TargetState.Failed, b.State);
		}

		[TestCategory("Functional")]
		[TestMethod]
		public void TargetRun_should_eval_state_as_partially_succeeded_if_at_least_one_dependent_partially_succeeded()
		{
			// arrange			
			"a".AsTarget()
				.Do(() => { throw new TerminateTargetException(); })
				.SkipErrors();
			var b = "b".AsTarget()
				.Do(() => { })
				.DependsOn("a");

			// act
			b.Run();

			// assert
			Assert.AreEqual(TargetState.PartiallySucceeded, b.State);
		}

		[TestCategory("Functional")]
		[TestMethod]
		public void TargetRun_should_eval_state_as_partially_succeeded_if_self_failed_and_skip_errors_enabled()
		{
			// arrange						
			var a = "a".AsTarget()
				.Do(() => { throw new TerminateTargetException(); })
				.SkipErrors();

			// act
			a.Run();

			// assert
			Assert.AreEqual(TargetState.PartiallySucceeded, a.State);
		}

		[TestCategory("Functional")]
		[TestMethod]
		public void TargetRun_should_eval_state_as_failed_if_any_warning_and_fail_by_warnings_enabled()
		{
			// arrange			
			var a = "a".AsTarget()
				.Do(() => Trace.Warn("Fatal Warning"))
				.FailIfAnyWarning();

			// act
			SafeOp.Try(a.Run);
			
			// assert
			Assert.AreEqual(TargetState.Failed, a.State);
		}

		[TestCategory("Functional")]
		[TestMethod]
		public void TargetRun_should_eval_state_as_partially_succeeded_if_any_warning_and_fail_by_warnings_and_skip_errors_enabled()
		{
			// arrange			
			var a = "a".AsTarget()
				.Do(() => Trace.Warn("Fatal Warning"))
				.FailIfAnyWarning()
				.SkipErrors();

			// act
			a.Run();
			
			// assert
			Assert.AreEqual(TargetState.PartiallySucceeded, a.State);
		}

		[TestCategory("Functional")]
		[TestMethod]
		public void TargetRun_should_eval_state_as_partially_succeeded_if_any_warning_and_partial_succeed_by_warnings_enabled()
		{
			// arrange			
			var a = "a".AsTarget()
				.Do(() => Trace.Warn("Fatal Warning"))
				.PartialSucceedIfAnyWarning();

			// act
			a.Run();

			// assert
			Assert.AreEqual(TargetState.PartiallySucceeded, a.State);
		}
	}
}