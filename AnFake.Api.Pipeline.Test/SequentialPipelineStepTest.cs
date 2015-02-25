using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhino.Mocks;

namespace AnFake.Api.Pipeline.Test
{
	[TestClass]
	public class SequentialPipelineStepTest
	{
		internal IPipelineImplementor Impl;
		internal Pipeline Pipeline;
		internal PipelineStep SubStep1;
		internal PipelineStep SubStep2;
		internal SequentialPipelineStep TestStep;

		[TestInitialize]
		public void Initialize()
		{
			Impl = MockRepository.GenerateMock<IPipelineImplementor>();
			SubStep1 = MockRepository.GenerateMock<PipelineStep>();
			SubStep2 = MockRepository.GenerateMock<PipelineStep>();
			TestStep = new SequentialPipelineStep(SubStep1, SubStep2);
			Pipeline = new Pipeline(TestStep, Impl);
		}
		
		[TestCategory("Unit")]
		[TestMethod]
		public void SequentialStep_should_run_first_substep_only()
		{
			// arrange
			SubStep1.Stub(x => x.Step(Pipeline))
				.Return(PipelineStepStatus.Queued);
			
			// act
			var status = TestStep.Step(Pipeline);

			// assert
			Assert.AreEqual(PipelineStepStatus.Queued, status);
			SubStep1.AssertWasCalled(x => x.Step(Pipeline));			
		}

		[TestCategory("Unit")]
		[TestMethod]
		public void SequentialStep_should_not_run_second_substep_while_first_is_queued()
		{
			// arrange
			SubStep1.Stub(x => x.Step(Pipeline))
				.Return(PipelineStepStatus.Queued);

			// act
			TestStep.Step(Pipeline);

			// assert
			SubStep2.AssertWasNotCalled(x => x.Step(Pipeline));
		}

		[TestCategory("Unit")]
		[TestMethod]
		public void SequentialStep_should_not_run_second_substep_while_first_is_in_progress()
		{
			// arrange
			SubStep1.Stub(x => x.Step(Pipeline))
				.Return(PipelineStepStatus.InProgress);

			// act
			TestStep.Step(Pipeline);

			// assert	
			SubStep2.AssertWasNotCalled(x => x.Step(Pipeline));
		}

		[TestCategory("Unit")]
		[TestMethod]
		public void SequentialStep_should_not_run_second_substep_when_first_failed()
		{
			// arrange
			SubStep1.Stub(x => x.Step(Pipeline))
				.Return(PipelineStepStatus.Failed);

			// act
			TestStep.Step(Pipeline);

			// assert	
			SubStep2.AssertWasNotCalled(x => x.Step(Pipeline));
		}

		[TestCategory("Unit")]
		[TestMethod]
		public void SequentialStep_should_run_second_substep_when_first_partially_succeeded()
		{
			// arrange
			SubStep1.Stub(x => x.Step(Pipeline))
				.Return(PipelineStepStatus.PartiallySucceeded);
			SubStep2.Stub(x => x.Step(Pipeline))
				.Return(PipelineStepStatus.InProgress);

			// act
			TestStep.Step(Pipeline);

			// assert			
			SubStep2.AssertWasCalled(x => x.Step(Pipeline));
		}

		[TestCategory("Unit")]
		[TestMethod]
		public void SequentialStep_should_run_second_substep_when_first_succeeded()
		{
			// arrange
			SubStep1.Stub(x => x.Step(Pipeline))
				.Return(PipelineStepStatus.Succeeded);
			SubStep2.Stub(x => x.Step(Pipeline))
				.Return(PipelineStepStatus.InProgress);

			// act
			TestStep.Step(Pipeline);

			// assert
			SubStep2.AssertWasCalled(x => x.Step(Pipeline));
		}

		[TestCategory("Unit")]
		[TestMethod]
		public void SequentialStep_should_return_queued_if_first_queued()
		{
			// arrange
			SubStep1.Stub(x => x.Step(Pipeline))
				.Return(PipelineStepStatus.Queued);

			// act
			var status = TestStep.Step(Pipeline);

			// assert
			Assert.AreEqual(PipelineStepStatus.Queued, status);			
		}

		[TestCategory("Unit")]
		[TestMethod]
		public void SequentialStep_should_return_in_progress_if_first_in_progress()
		{
			// arrange
			SubStep1.Stub(x => x.Step(Pipeline))
				.Return(PipelineStepStatus.InProgress);

			// act
			var status = TestStep.Step(Pipeline);

			// assert
			Assert.AreEqual(PipelineStepStatus.InProgress, status);
		}

		[TestCategory("Unit")]
		[TestMethod]
		public void SequentialStep_should_return_failed_if_first_failed()
		{
			// arrange
			SubStep1.Stub(x => x.Step(Pipeline))
				.Return(PipelineStepStatus.Failed);
			SubStep2.Stub(x => x.Step(Pipeline))
				.Return(PipelineStepStatus.Queued);

			// act
			var status = TestStep.Step(Pipeline);

			// assert
			Assert.AreEqual(PipelineStepStatus.Failed, status);
		}

		[TestCategory("Unit")]
		[TestMethod]
		public void SequentialStep_should_return_queued_if_second_queued()
		{
			// arrange
			SubStep1.Stub(x => x.Step(Pipeline))
				.Return(PipelineStepStatus.Succeeded);
			SubStep2.Stub(x => x.Step(Pipeline))
				.Return(PipelineStepStatus.Queued);

			// act
			var status = TestStep.Step(Pipeline);

			// assert
			Assert.AreEqual(PipelineStepStatus.Queued, status);
		}

		[TestCategory("Unit")]
		[TestMethod]
		public void SequentialStep_should_return_in_progress_if_second_in_progress()
		{
			// arrange
			SubStep1.Stub(x => x.Step(Pipeline))
				.Return(PipelineStepStatus.Succeeded);
			SubStep2.Stub(x => x.Step(Pipeline))
				.Return(PipelineStepStatus.InProgress);

			// act
			var status = TestStep.Step(Pipeline);

			// assert
			Assert.AreEqual(PipelineStepStatus.InProgress, status);
		}

		[TestCategory("Unit")]
		[TestMethod]
		public void SequentialStep_should_return_failed_if_second_failed()
		{
			// arrange
			SubStep1.Stub(x => x.Step(Pipeline))
				.Return(PipelineStepStatus.Succeeded);
			SubStep2.Stub(x => x.Step(Pipeline))
				.Return(PipelineStepStatus.Failed);

			// act
			var status = TestStep.Step(Pipeline);

			// assert
			Assert.AreEqual(PipelineStepStatus.Failed, status);
		}

		[TestCategory("Unit")]
		[TestMethod]
		public void SequentialStep_should_return_partial_success_if_first_partially_succeeded()
		{
			// arrange
			SubStep1.Stub(x => x.Step(Pipeline))
				.Return(PipelineStepStatus.PartiallySucceeded);
			SubStep2.Stub(x => x.Step(Pipeline))
				.Return(PipelineStepStatus.Succeeded);

			// act
			var status = TestStep.Step(Pipeline);

			// assert
			Assert.AreEqual(PipelineStepStatus.PartiallySucceeded, status);
		}

		[TestCategory("Unit")]
		[TestMethod]
		public void SequentialStep_should_return_partial_success_if_second_partially_succeeded()
		{
			// arrange
			SubStep1.Stub(x => x.Step(Pipeline))
				.Return(PipelineStepStatus.Succeeded);
			SubStep2.Stub(x => x.Step(Pipeline))
				.Return(PipelineStepStatus.PartiallySucceeded);

			// act
			var status = TestStep.Step(Pipeline);

			// assert
			Assert.AreEqual(PipelineStepStatus.PartiallySucceeded, status);
		}

		[TestCategory("Unit")]
		[TestMethod]
		public void SequentialStep_should_return_success_if_both_succeeded()
		{
			// arrange
			SubStep1.Stub(x => x.Step(Pipeline))
				.Return(PipelineStepStatus.Succeeded);
			SubStep2.Stub(x => x.Step(Pipeline))
				.Return(PipelineStepStatus.Succeeded);

			// act
			var status = TestStep.Step(Pipeline);

			// assert
			Assert.AreEqual(PipelineStepStatus.Succeeded, status);
		}
	}
}