using System.Collections.Generic;
using AnFake.Api.Pipeline.Antlr;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;

namespace AnFake.Api.Pipeline
{
	internal static class PipelineCompiler
	{
		private sealed class CompilationListener : PipelineBaseListener
		{
			private readonly Stack<PipelineStep> _steps = new Stack<PipelineStep>();

			public PipelineStep InitialStep
			{
				get { return _steps.Peek(); }
			}

			public override void ExitInternalSequentialStep(PipelineParser.InternalSequentialStepContext ctx)
			{
				var second = _steps.Pop();
				var first = _steps.Pop();

				_steps.Push(new SequentialPipelineStep(first, second));
			}

			public override void ExitInternalParallelStep(PipelineParser.InternalParallelStepContext ctx)
			{
				var second = _steps.Pop();
				var first = _steps.Pop();

				_steps.Push(new ParallelPipelineStep(first, second));
			}

			public override void ExitOptionalBuildRun(PipelineParser.OptionalBuildRunContext ctx)
			{
				var inner = _steps.Pop();

				_steps.Push(new OptionalPipelineStep(inner));
			}
			
			public override void EnterInternalBuildRunVoid(PipelineParser.InternalBuildRunVoidContext ctx)
			{
				_steps.Push(
					new QueueBuildStep(
						Unquote(ctx.buildRunName()), 
						null, 
						null));
			}

			public override void EnterInternalBuildRunIn(PipelineParser.InternalBuildRunInContext ctx)
			{
				_steps.Push(
					new QueueBuildStep(
						Unquote(ctx.buildRunName()),
						ctx.Identifier().GetText(),
						null));
			}

			public override void EnterInternalBuildRunOut(PipelineParser.InternalBuildRunOutContext ctx)
			{
				_steps.Push(
					new QueueBuildStep(
						Unquote(ctx.buildRunName()),
						null,
						ctx.Identifier().GetText()));
			}

			public override void EnterInternalBuildRunInOut(PipelineParser.InternalBuildRunInOutContext ctx)
			{
				_steps.Push(
					new QueueBuildStep(
						Unquote(ctx.buildRunName()),
						ctx.Identifier(0).GetText(),
						ctx.Identifier(1).GetText()));
			}

			private static string Unquote(PipelineParser.BuildRunNameContext ctx)
			{
				if (ctx.Identifier() != null)
					return ctx.Identifier().GetText();

				var quotedId = ctx.QuotedIdentifier().GetText();
				return quotedId.Substring(1, quotedId.Length - 2);
			}
		}

		public static PipelineStep Compile(string pipelineDef)
		{
			var lexer = new PipelineLexer(new AntlrInputStream(pipelineDef));
			var tokens = new CommonTokenStream(lexer);
			var parser = new PipelineParser(tokens);
			var tree = parser.pipeline();

			var walker = new ParseTreeWalker();
			var listener = new CompilationListener();
			walker.Walk(listener, tree);

			return listener.InitialStep;
		}
	}	
}