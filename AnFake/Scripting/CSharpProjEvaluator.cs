using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using AnFake.Api;
using AnFake.Core;
using AnFake.Core.Exceptions;
using AnFake.Csx;

namespace AnFake.Scripting
{
	internal class CSharpProjEvaluator : IScriptEvaluator
	{
		private sealed class BuildSpec
		{
			private readonly Type _type;
			private object _instance;

			public BuildSpec(Type type)
			{
				_type = type;
			}

			private object Instance
			{
				get { return _instance ?? (_instance = Activator.CreateInstance(_type)); }
			}

			public IEnumerable<MethodInfo> GetDoMethods()
			{
				return _type.GetMethods()
					.Where(x => x.GetCustomAttribute(typeof (TargetAttribute), false) != null);
			}

			public IEnumerable<MethodInfo> GetFinallyMethods()
			{
				return _type.GetMethods()
					.Where(x => x.GetCustomAttribute(typeof(TargetFinallyAttribute), false) != null);
			}

			public IEnumerable<MethodInfo> GetOnFailureMethods()
			{
				return _type.GetMethods()
					.Where(x => x.GetCustomAttribute(typeof(TargetOnFailureAttribute), false) != null);
			}

			public bool IsSkipErrors(MethodInfo method)
			{
				return method.GetCustomAttribute(typeof (SkipErrorsAttribute), false) != null;
			}

			public bool IsFailIfAnyWarning(MethodInfo method)
			{
				return method.GetCustomAttribute(typeof(FailIfAnyWarningAttribute), false) != null;
			}

			public Action CreateDoAction(MethodInfo method)
			{
				if (method.ReturnType != typeof(void) || method.GetParameters().Length > 0)
					throw new EvaluationException(String.Format("Method marked as [Target] must follow the convention: 'public [static] void <TargetName>()', but found '{0}'.", Format(method)));

				return (Action)(method.IsStatic
					? method.CreateDelegate(typeof(Action))
					: method.CreateDelegate(typeof(Action), Instance));				
			}

			public Delegate CreateFinallyAction(MethodInfo method)
			{
				return CreateSpecialAction(method, "Finally");
			}

			public Delegate CreateOnFailureAction(MethodInfo method)
			{
				return CreateSpecialAction(method, "OnFailure");
			}

			public string GetTargetName(MethodInfo method)
			{
				if (method.Name.EndsWith("Finally"))
					return method.Name.Substring(0, method.Name.Length - 7);

				if (method.Name.EndsWith("OnFailure"))
					return method.Name.Substring(0, method.Name.Length - 9);

				return method.Name;
			}

			private Delegate CreateSpecialAction(MethodInfo method, string specialName)
			{
				if (!method.Name.EndsWith(specialName))
					throw new EvaluationException(
						String.Format(
							"Name of method marked as [Target{0}] must follow the convention: '<TargetName>{0}', but found '{1}'.",
							specialName, Format(method)
						)
					);

				var prms = method.GetParameters();
				if (method.ReturnType != typeof(void) || prms.Length > 1)
					throw new EvaluationException(
						String.Format(
							"Method marked as [Target{0}] must follow the convention: 'public [static] void <TargetName>{0}([ExecutionReason reason])', but found '{1}'.",
							specialName, Format(method)
						)
					);

				if (prms.Length == 1 && prms[0].ParameterType != typeof(Target.ExecutionReason))
					throw new EvaluationException(
						String.Format(
							"Parameter type for method marked as [Target{0}] must be ExecutionReason, but found '{1}'.",
							specialName, Format(method)
						)
					);

				if (prms.Length == 0)
				{
					return method.IsStatic
						? method.CreateDelegate(typeof(Action))
						: method.CreateDelegate(typeof(Action), Instance);
				}

				return method.IsStatic
					? method.CreateDelegate(typeof(Action<Target.ExecutionReason>))
					: method.CreateDelegate(typeof(Action<Target.ExecutionReason>), Instance);
			}

			private static string Format(MethodInfo method)
			{
				var sb= new StringBuilder();

				// ReSharper disable once PossibleNullReferenceException
				sb.Append("class ").Append(method.DeclaringType.Name).Append("{ ");
				
				if (method.IsPublic)
				{
					sb.Append("public");
				}
				else if (method.IsPrivate)
				{
					sb.Append("private");
				}

				sb.Append(' ');

				if (method.IsStatic)
				{
					sb.Append("static ");
				}

				sb.Append(method.ReturnType != typeof (void) ? method.ReturnType.Name : "void");

				sb.Append(' ');

				sb.Append(method.Name);

				var prms = method.GetParameters();
				sb.Append('(');
				sb.Append(String.Join(", ", prms.Select(p => p.ParameterType.Name + " " + p.Name)));
				sb.Append(')');

				sb.Append(" }");

				return sb.ToString();
			}
		}

		public FileSystemPath GetBasePath(FileItem script)
		{
			return script.Folder.Parent;
		}

		public void Evaluate(FileItem script, bool debug)
		{			
			var confName = debug ? "Debug" : "Release";

			Trace.Info("CSPROJ: Compiling build script with MsBuild...");
			MsBuild.Build(script, p => p.Properties["Configuration"] = confName);

			var assemblyPath = script.Folder / "bin" / confName / Os.dll(script.NameWithoutExt);
			
			Trace.InfoFormat("CSPROJ: Loading assembly '{0}'...", assemblyPath);
			var assembly = Assembly.LoadFile(assemblyPath.Full);

			Trace.Info("CSPROJ: Searching for targets...");
			foreach (var type in assembly.GetExportedTypes())
			{
				ProcessBuildSpec(new BuildSpec(type));
			}
			Trace.InfoFormat("CSPROJ: {0} target(s) found.", Target.Count);

			AnFakeException.ScriptSource = new ScriptSourceInfo(script.Name);
		}

		private static void ProcessBuildSpec(BuildSpec spec)
		{
			foreach (var doMethod in spec.GetDoMethods())
			{
				var target = spec.GetTargetName(doMethod).AsTarget();

				target.Do(spec.CreateDoAction(doMethod));

				if (spec.IsSkipErrors(doMethod))
				{
					target.SkipErrors();
				}

				if (spec.IsFailIfAnyWarning(doMethod))
				{
					target.FailIfAnyWarning();
				}
			}

			foreach (var failureMethod in spec.GetOnFailureMethods())
			{
				spec.GetTargetName(failureMethod)
					.AsTarget()
					.OnFailure((dynamic)spec.CreateOnFailureAction(failureMethod));
			}

			foreach (var finallyMethod in spec.GetFinallyMethods())
			{
				spec.GetTargetName(finallyMethod)
					.AsTarget()
					.Finally((dynamic)spec.CreateFinallyAction(finallyMethod));
			}			
		}
	}
}