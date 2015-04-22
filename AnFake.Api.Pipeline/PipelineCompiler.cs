using System.Collections.Generic;
using System.Linq;
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
				var step = new QueueBuildStep(
					Unquote(ctx.buildRunName()),
					GetPipeIn(ctx.buildRunParams()),
					null);

				step.Parameters.AddRange(
					GetCustomParams(ctx.buildRunParams()));
				
				_steps.Push(step);
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
				var step = new QueueBuildStep(
					Unquote(ctx.buildRunName()),
					GetPipeIn(ctx.buildRunParams()),
					ctx.Identifier().GetText());

				step.Parameters.AddRange(
					GetCustomParams(ctx.buildRunParams()));

				_steps.Push(step);
			}

			private static string Unquote(PipelineParser.BuildRunNameContext ctx)
			{
				if (ctx.Identifier() != null)
					return ctx.Identifier().GetText();

				var quotedId = ctx.QuotedIdentifier().GetText();
				return quotedId.Substring(1, quotedId.Length - 2);
			}

			private static string GetPipeIn(PipelineParser.BuildRunParamsContext ctx)
			{
				return ctx.Identifier().GetText();
			}

			private static IEnumerable<string> GetCustomParams(PipelineParser.BuildRunParamsContext ctx)
			{
				return ctx.QuotedIdentifier()
					.Select(param => param.GetText())
					.Select(quotedVal => quotedVal.Substring(1, quotedVal.Length - 2));
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