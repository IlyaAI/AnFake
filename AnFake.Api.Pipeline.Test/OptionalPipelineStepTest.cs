using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhino.Mocks;

namespace AnFake.Api.Pipeline.Test
{
	[TestClass]
	public class OptionalPipelineStepTest
	{
		internal IPipelineImplementor Impl;
		internal Pipeline Pipeline;
		internal PipelineStep SubStep;		
		internal OptionalPipelineStep TestStep;

		[TestInitialize]
		public void Initialize()
		{
			Impl = MockRepository.GenerateMock<IPipelineImplementor>();
			SubStep = MockRepository.GenerateMock<PipelineStep>();			
			TestStep = new OptionalPipelineStep(SubStep);
			Pipeline = new Pipeline(TestStep, Impl);
		}

		[TestCategory("Unit")]
		[TestMethod]
		public void OptionalStep_should_run_substep()
		{
			// arrange
			SubStep.Stub(x => x.Step(Pipeline))
				.Return(PipelineStepStatus.Queued);
			
			// act
			TestStep.Step(Pipeline);

			// assert
			SubStep.AssertWasCalled(x => x.Step(Pipeline));			
		}

		[TestCategory("Unit")]
		[TestMethod]
		public void OptionalStep_should_return_queued_if_substep_queued()
		{
			// arrange
			SubStep.Stub(x => x.Step(Pipeline))
				.Return(PipelineStepStatus.Queued);
			
			// act
			var status = TestStep.Step(Pipeline);

			// assert
			Assert.AreEqual(PipelineStepStatus.Queued, status);
		}

		[TestCategory("Unit")]
		[TestMethod]
		public void OptionalStep_should_return_in_progress_if_substep_in_progress()
		{
			// arrange
			SubStep.Stub(x => x.Step(Pipeline))
				.Return(PipelineStepStatus.InProgress);
			
			// act
			var status = TestStep.Step(Pipeline);

			// assert
			Assert.AreEqual(PipelineStepStatus.InProgress, status);
		}

		[TestCategory("Unit")]
		[TestMethod]
		public void OptionalStep_should_return_partial_success_if_substep_failed()
		{
			// arrange
			SubStep.Stub(x => x.Step(Pipeline))
				.Return(PipelineStepStatus.Failed);
			
			// act
			var status = TestStep.Step(Pipeline);

			// assert
			Assert.AreEqual(PipelineStepStatus.PartiallySucceeded, status);
		}
		
		[TestCategory("Unit")]
		[TestMethod]
		public void OptionalStep_should_return_partial_success_if_substep_partially_succeeded()
		{
			// arrange
			SubStep.Stub(x => x.Step(Pipeline))
				.Return(PipelineStepStatus.PartiallySucceeded);
			
			// act
			var status = TestStep.Step(Pipeline);

			// assert
			Assert.AreEqual(PipelineStepStatus.PartiallySucceeded, status);
		}

		[TestCategory("Unit")]
		[TestMethod]
		public void OptionalStep_should_return_success_if_substep_succeeded()
		{
			// arrange
			SubStep.Stub(x => x.Step(Pipeline))
				.Return(PipelineStepStatus.Succeeded);
			
			// act
			var status = TestStep.Step(Pipeline);

			// assert
			Assert.AreEqual(PipelineStepStatus.Succeeded, status);
		}
	}
}