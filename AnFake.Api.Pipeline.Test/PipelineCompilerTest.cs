using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AnFake.Api.Pipeline.Test
{
	[TestClass]
	public class PipelineCompilerTest
	{
		[TestMethod]
		public void PipelineCompiler_should_compile_single_buildrun()
		{
			// arrange
			const string src = "A";

			// act
			var step = PipelineCompiler.Compile(src) as BuildRun;

			// assert
			Assert.IsNotNull(step);
			Assert.AreEqual("A", step.Name);
		}

		[TestMethod]
		public void PipelineCompiler_should_compile_single_buildrun_with_alias()
		{
			// arrange
			const string src = "A as a";

			// act
			var step = PipelineCompiler.Compile(src) as BuildRun;

			// assert
			Assert.IsNotNull(step);
			Assert.AreEqual("A", step.Name);
			Assert.AreEqual("a", step.PipeOut);
		}

		[TestMethod]
		public void PipelineCompiler_should_compile_single_buildrun_with_param()
		{
			// arrange
			const string src = "A(b)";

			// act
			var step = PipelineCompiler.Compile(src) as BuildRun;

			// assert
			Assert.IsNotNull(step);
			Assert.AreEqual("A", step.Name);
			Assert.AreEqual("b", step.PipeIn);
		}

		[TestMethod]
		public void PipelineCompiler_should_compile_sequential_run()
		{
			// arrange
			const string src = "A -> B";

			// act
			var step = PipelineCompiler.Compile(src) as SequentialPipelineStep;

			// assert
			Assert.IsNotNull(step);
			Assert.AreEqual("A", ((BuildRun)step.First).Name);
			Assert.AreEqual("B", ((BuildRun)step.Second).Name);
		}

		[TestMethod]
		public void PipelineCompiler_should_compile_parallel_run()
		{
			// arrange
			const string src = "A => B";

			// act
			var step = PipelineCompiler.Compile(src) as ParallelPipelineStep;

			// assert
			Assert.IsNotNull(step);
			Assert.AreEqual("A", ((BuildRun)step.First).Name);
			Assert.AreEqual("B", ((BuildRun)step.Second).Name);
		}

		[TestMethod]
		public void PipelineCompiler_should_compile_parallel_then_sequential_run()
		{
			// arrange
			const string src = "A => B -> C";

			// act
			var step = PipelineCompiler.Compile(src) as ParallelPipelineStep;

			// assert
			Assert.IsNotNull(step);
			Assert.AreEqual("A", ((BuildRun)step.First).Name);

			var subStep = step.Second as SequentialPipelineStep;
			Assert.IsNotNull(subStep);
			Assert.AreEqual("B", ((BuildRun)subStep.First).Name);
			Assert.AreEqual("C", ((BuildRun)subStep.Second).Name);
		}

		[TestMethod]
		public void PipelineCompiler_should_compile_parallel_with_nested_sequential_run()
		{
			// arrange
			const string src = "(A -> B) => C";

			// act
			var step = PipelineCompiler.Compile(src) as ParallelPipelineStep;

			// assert
			Assert.IsNotNull(step);
			Assert.AreEqual("C", ((BuildRun)step.Second).Name);

			var subStep = step.First as SequentialPipelineStep;
			Assert.IsNotNull(subStep);
			Assert.AreEqual("A", ((BuildRun)subStep.First).Name);
			Assert.AreEqual("B", ((BuildRun)subStep.Second).Name);
		}

		[TestMethod]
		public void PipelineCompiler_should_compile_three_sequential_runs()
		{
			// arrange
			const string src = "A -> B -> C";

			// act
			var step = PipelineCompiler.Compile(src) as SequentialPipelineStep;

			// assert
			Assert.IsNotNull(step);
			Assert.AreEqual("C", ((BuildRun)step.Second).Name);

			var subStep = step.First as SequentialPipelineStep;
			Assert.IsNotNull(subStep);
			Assert.AreEqual("A", ((BuildRun)subStep.First).Name);
			Assert.AreEqual("B", ((BuildRun)subStep.Second).Name);
		}
	}
}