//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     ANTLR Version: 4.5
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

// Generated from Pipeline.g4 by ANTLR 4.5

// Unreachable code detected
#pragma warning disable 0162
// The variable '...' is assigned but its value is never used
#pragma warning disable 0219
// Missing XML comment for publicly visible type or member '...'
#pragma warning disable 1591

namespace AnFake.Api.Pipeline.Antlr {
using Antlr4.Runtime.Misc;
using IParseTreeListener = Antlr4.Runtime.Tree.IParseTreeListener;
using IToken = Antlr4.Runtime.IToken;

/// <summary>
/// This interface defines a complete listener for a parse tree produced by
/// <see cref="PipelineParser"/>.
/// </summary>
[System.CodeDom.Compiler.GeneratedCode("ANTLR", "4.5")]
[System.CLSCompliant(false)]
public interface IPipelineListener : IParseTreeListener {
	/// <summary>
	/// Enter a parse tree produced by <see cref="PipelineParser.pipeline"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	void EnterPipeline([NotNull] PipelineParser.PipelineContext context);
	/// <summary>
	/// Exit a parse tree produced by <see cref="PipelineParser.pipeline"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	void ExitPipeline([NotNull] PipelineParser.PipelineContext context);
	/// <summary>
	/// Enter a parse tree produced by the <c>InternalUnaryStep</c>
	/// labeled alternative in <see cref="PipelineParser.step"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	void EnterInternalUnaryStep([NotNull] PipelineParser.InternalUnaryStepContext context);
	/// <summary>
	/// Exit a parse tree produced by the <c>InternalUnaryStep</c>
	/// labeled alternative in <see cref="PipelineParser.step"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	void ExitInternalUnaryStep([NotNull] PipelineParser.InternalUnaryStepContext context);
	/// <summary>
	/// Enter a parse tree produced by the <c>InternalSequentialStep</c>
	/// labeled alternative in <see cref="PipelineParser.step"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	void EnterInternalSequentialStep([NotNull] PipelineParser.InternalSequentialStepContext context);
	/// <summary>
	/// Exit a parse tree produced by the <c>InternalSequentialStep</c>
	/// labeled alternative in <see cref="PipelineParser.step"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	void ExitInternalSequentialStep([NotNull] PipelineParser.InternalSequentialStepContext context);
	/// <summary>
	/// Enter a parse tree produced by the <c>InternalParallelStep</c>
	/// labeled alternative in <see cref="PipelineParser.step"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	void EnterInternalParallelStep([NotNull] PipelineParser.InternalParallelStepContext context);
	/// <summary>
	/// Exit a parse tree produced by the <c>InternalParallelStep</c>
	/// labeled alternative in <see cref="PipelineParser.step"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	void ExitInternalParallelStep([NotNull] PipelineParser.InternalParallelStepContext context);
	/// <summary>
	/// Enter a parse tree produced by <see cref="PipelineParser.unaryStep"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	void EnterUnaryStep([NotNull] PipelineParser.UnaryStepContext context);
	/// <summary>
	/// Exit a parse tree produced by <see cref="PipelineParser.unaryStep"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	void ExitUnaryStep([NotNull] PipelineParser.UnaryStepContext context);
	/// <summary>
	/// Enter a parse tree produced by <see cref="PipelineParser.optionalBuildRun"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	void EnterOptionalBuildRun([NotNull] PipelineParser.OptionalBuildRunContext context);
	/// <summary>
	/// Exit a parse tree produced by <see cref="PipelineParser.optionalBuildRun"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	void ExitOptionalBuildRun([NotNull] PipelineParser.OptionalBuildRunContext context);
	/// <summary>
	/// Enter a parse tree produced by the <c>InternalBuildRunVoid</c>
	/// labeled alternative in <see cref="PipelineParser.buildRun"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	void EnterInternalBuildRunVoid([NotNull] PipelineParser.InternalBuildRunVoidContext context);
	/// <summary>
	/// Exit a parse tree produced by the <c>InternalBuildRunVoid</c>
	/// labeled alternative in <see cref="PipelineParser.buildRun"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	void ExitInternalBuildRunVoid([NotNull] PipelineParser.InternalBuildRunVoidContext context);
	/// <summary>
	/// Enter a parse tree produced by the <c>InternalBuildRunIn</c>
	/// labeled alternative in <see cref="PipelineParser.buildRun"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	void EnterInternalBuildRunIn([NotNull] PipelineParser.InternalBuildRunInContext context);
	/// <summary>
	/// Exit a parse tree produced by the <c>InternalBuildRunIn</c>
	/// labeled alternative in <see cref="PipelineParser.buildRun"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	void ExitInternalBuildRunIn([NotNull] PipelineParser.InternalBuildRunInContext context);
	/// <summary>
	/// Enter a parse tree produced by the <c>InternalBuildRunOut</c>
	/// labeled alternative in <see cref="PipelineParser.buildRun"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	void EnterInternalBuildRunOut([NotNull] PipelineParser.InternalBuildRunOutContext context);
	/// <summary>
	/// Exit a parse tree produced by the <c>InternalBuildRunOut</c>
	/// labeled alternative in <see cref="PipelineParser.buildRun"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	void ExitInternalBuildRunOut([NotNull] PipelineParser.InternalBuildRunOutContext context);
	/// <summary>
	/// Enter a parse tree produced by the <c>InternalBuildRunInOut</c>
	/// labeled alternative in <see cref="PipelineParser.buildRun"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	void EnterInternalBuildRunInOut([NotNull] PipelineParser.InternalBuildRunInOutContext context);
	/// <summary>
	/// Exit a parse tree produced by the <c>InternalBuildRunInOut</c>
	/// labeled alternative in <see cref="PipelineParser.buildRun"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	void ExitInternalBuildRunInOut([NotNull] PipelineParser.InternalBuildRunInOutContext context);
	/// <summary>
	/// Enter a parse tree produced by <see cref="PipelineParser.buildRunName"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	void EnterBuildRunName([NotNull] PipelineParser.BuildRunNameContext context);
	/// <summary>
	/// Exit a parse tree produced by <see cref="PipelineParser.buildRunName"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	void ExitBuildRunName([NotNull] PipelineParser.BuildRunNameContext context);
	/// <summary>
	/// Enter a parse tree produced by <see cref="PipelineParser.buildRunParams"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	void EnterBuildRunParams([NotNull] PipelineParser.BuildRunParamsContext context);
	/// <summary>
	/// Exit a parse tree produced by <see cref="PipelineParser.buildRunParams"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	void ExitBuildRunParams([NotNull] PipelineParser.BuildRunParamsContext context);
}
} // namespace AnFake.Api.Pipeline.Antlr
