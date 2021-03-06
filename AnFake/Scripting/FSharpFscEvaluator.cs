﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using AnFake.Api;
using AnFake.Core;
using AnFake.Core.Exceptions;
using Microsoft.FSharp.Compiler;
using Microsoft.FSharp.Compiler.SimpleSourceCodeServices;

namespace AnFake.Scripting
{
	internal static class EmbeddedFsxCompiler
	{
		private const string TempAnFakeFsc = "[Temp]/AnFake.Fsc";
		private const string RootTypeName = "BuildScript";
		private const string VersionPropertyName = "__anfake_fsc_evaluator_version";
		private const string InternalVersion = "1.0";

		private static readonly Dictionary<string, FileItem> ReferencedAssemblies = new Dictionary<string, FileItem>();

		private static readonly ISet<string> PredefinedReferences = new HashSet<string>
		{
			"mscorlib",
			"System",
			"System.Core",
			"System.Xml"
		};		

		public sealed class CompiledScript
		{
			public CompiledScript(FileItem assembly, FileItem source, int linesOffset, string moduleName)
			{
				Assembly = assembly;
				Source = source;
				LinesOffset = linesOffset;
				ModuleName = moduleName;
			}

			public FileItem Assembly { get; private set; }

			public FileItem Source { get; private set; }

			public int LinesOffset { get; private set; }

			public string ModuleName { get; private set; }

			public void Evaluate()
			{
				var versionProp = System.Reflection.Assembly
					.LoadFile(Assembly.Path.Full)
					.GetType(RootTypeName, true)
					.GetProperty(VersionPropertyName);

				if (versionProp == null)
					throw new EvaluationException(
						String.Format(
							"Property '{0}.{1}' not found in pre-compiled assembly '{2}'.",
							RootTypeName, VersionPropertyName, Assembly));

				// getting value of static property also triggers script evaluation
				try
				{
					var ver = versionProp.GetValue(null);
					if (!InternalVersion.AsVersion().Equals(ver))
						throw new EvaluationException(
							String.Format(
								"Pre-compiled assembly has incompatible version. Expected {0}, actual {1}, assembly path '{2}'.",
								InternalVersion, ver, Assembly));
				}
				catch (TargetInvocationException e)
				{
					Exception anfakeEx = e;
					while (anfakeEx != null && !(anfakeEx is AnFakeException))
					{
						anfakeEx = anfakeEx.InnerException;
					}

					throw anfakeEx ?? e;
				}
			}
		}

		private class PseudoProject
		{
			public string Code { get; private set; }
			public FileItem[] References { get; private set; }
			public string Name { get; private set; }
			public int LinesOffset { get; private set; }
			public string ModuleName { get; private set; }

			public FileItem Input
			{
				get { return (TempAnFakeFsc.AsPath() / Name + ".g.fs").AsFile(); }
			}

			public FileItem Output
			{
				get { return (TempAnFakeFsc.AsPath() / Name + ".dll").AsFile(); }
			}

			public PseudoProject(FileItem script, string code, FileItem[] references, int linesOffset, string moduleName)
			{
				Code = code;
				References = references;
				LinesOffset = linesOffset;
				ModuleName = moduleName;

				var memStream = new MemoryStream();
				var writer = new StreamWriter(memStream);
				writer.Write(code);
				foreach (var reference in references)
				{
					writer.Write(reference.Path.Spec);
				}
				writer.Flush();
				memStream.Seek(0, SeekOrigin.Begin);

				using (var md5 = MD5.Create())
				{
					var hash = md5.ComputeHash(memStream);

					var name = new StringBuilder(256);
					name.Append(script.Name).Append('.');
					foreach (var b in hash)
					{
						name.AppendFormat("{0:x}", b);
					}

					Name = name.ToString();
				}				
			}
		}

		static EmbeddedFsxCompiler()
		{
			AppDomain.CurrentDomain.AssemblyResolve += OnAssemblyResolve;
		}

		private static Assembly OnAssemblyResolve(object sender, ResolveEventArgs args)
		{
			var asmName = new AssemblyName(args.Name);

			FileItem asmFile;
			if (!ReferencedAssemblies.TryGetValue(asmName.Name, out asmFile))
				return null;

			try
			{
				return Assembly.LoadFile(asmFile.Path.Full);
			}
			catch (Exception)
			{
				// skip it
			}

			return null;
		}

		public static CompiledScript Compile(FileItem script)
		{
			Log.DebugFormat("FSC: Compilation requested: '{0}'.", script);

			var fsproj = GeneratePseudoProject(script);

			Log.DebugFormat("FSC: Looking for pre-compiled assembly: '{0}'.", fsproj.Output);

			if (!fsproj.Output.Exists())
			{
				Log.Debug("FSC: Pre-compiled assembly NOT found. Will compile.");

				DoCompile(fsproj);
			}
			else
			{
				Log.Debug("FSC: Pre-compiled assembly found. No compilation needed.");
			}

			ReferencedAssemblies[fsproj.Name] = fsproj.Output;
			foreach (var reference in fsproj.References)
			{
				if (reference.Name.StartsWith("AnFake.") 
					|| reference.Name.StartsWith("System.") 
					|| reference.Name.StartsWith("mscorlib."))
					continue;

				ReferencedAssemblies[reference.NameWithoutExt] = reference;
			}

			return new CompiledScript(fsproj.Output, fsproj.Input, fsproj.LinesOffset, fsproj.ModuleName);
		}

		public static void Cleanup()
		{
			var versionBarrier = new DateTime(2015, 10, 21, 4, 0, 0, DateTimeKind.Utc); // FSharp.Core version changed at this point
			var threshold = DateTime.UtcNow - TimeSpan.FromDays(7);
			if (threshold < versionBarrier)
			{
				threshold = versionBarrier;				
			}

			foreach (var file in "*".AsFileSetFrom(TempAnFakeFsc)
				.Where(file => file.Info.LastAccessTimeUtc < threshold))
			{
				File.Delete(file.Path.Full);
			}
		}

		private static void DoCompile(PseudoProject fsproj)
		{
			Log.Debug("FSC: Compiling...");
			
			Text.WriteTo(fsproj.Input, fsproj.Code);

			var args = new List<string>
			{
				"fsc.exe",				
				"-o",
				fsproj.Output.Path.Full,
				"-a",
				fsproj.Input.Path.Full,
				"--debug:pdbonly",
				"--optimize+",
				"--noframework"
			};

			foreach (var reference in fsproj.References)
			{
				args.Add("-r");
				args.Add(reference.Path.Spec);
			}

			Log.DebugFormat("FSC: {0}", String.Join(" ", args));

			var scs = new SimpleSourceCodeServices();
			var ret = scs.Compile(args.ToArray());

			foreach (var errorInfo in ret.Item1)
			{
				if (FSharpErrorSeverity.Error.Equals(errorInfo.Severity))
				{
					Log.ErrorFormat("[FSC-ERROR]: {0}", errorInfo);
				}
				else if (FSharpErrorSeverity.Warning.Equals(errorInfo.Severity))
				{
					Log.DebugFormat("[FSC-WARN ]: {0}", errorInfo);
				}
				else
				{
					Log.DebugFormat("[FSC-INFO ]: {0}", errorInfo);
				}
			}

			if (ret.Item2 != 0)
				throw new EvaluationException(String.Format("Unable to pre-compile script. fsc.exe exit code {0}.", ret.Item2));

			Log.DebugFormat("FSC: Pre-compiled assembly created: '{0}'.", fsproj.Output);
		}

		private static PseudoProject GeneratePseudoProject(FileItem script)
		{
			if (!script.Exists())
				throw new EvaluationException(String.Format("Script file '{0}' doesn't exist.", script));

			var refs = new List<FileItem>();
			refs.AddRange(
				AppDomain.CurrentDomain
					.GetAssemblies()
					.Where(x => PredefinedReferences.Contains(x.GetName().Name))
					.Select(x => x.Location.AsFile()));

			var moduleName = RootTypeName;

			var header = new StringBuilder(1024);
			GenerateHeader(header, RootTypeName);
			var linesOffset = 2;
			
			var body = new StringBuilder(10240);
			
			script.AsTextDoc().ForEachLine(
				line =>
				{
					if (line.Text.StartsWith("module "))
					{
						moduleName = line.Text.Substring(7).Trim();
						GenerateHeader(header, moduleName);						
						linesOffset = 1;
					} 
					else if (line.Text.StartsWith("#r "))
					{
						var value = GetDerictiveValue(line.Text, "r");
						refs.Add(
							value.StartsWith("System.")				// w/a to handle system assemblies
								? value.AsFile() 
								: (script.Folder/value).AsFile());

						body.Append("//").AppendLine(line.Text);
					}					
					else if (line.Text.StartsWith("#load "))
					{
						var compiledScript = Compile((script.Folder / GetDerictiveValue(line.Text, "load")).AsFile());
						refs.Add(compiledScript.Assembly);

						body.Append("//").AppendLine(line.Text);						
						body.Append("let ").Append(VersionPropertyName).Append('_').Append(compiledScript.ModuleName)
							.Append('=').Append(compiledScript.ModuleName).Append('.').Append(VersionPropertyName)
							.Append(" // force sub-module static constructor")
							.AppendLine();
					}
					else
					{
						body.AppendLine(line.Text);
					}
				});

			body.Append("let ").Append(VersionPropertyName).Append("=new Version(\"").Append(InternalVersion).AppendLine("\")");

			return new PseudoProject(script, header.ToString() + body, refs.ToArray(), linesOffset, moduleName);
		}

		private static void GenerateHeader(StringBuilder header, string moduleName)
		{
			header.Clear()
				.Append("module ").Append(moduleName).AppendLine()
				.Append("open System").AppendLine();
		}

		private static string GetDerictiveValue(string line, string derictiveName)
		{
			var beg = line.IndexOf('"');
			var end = line.LastIndexOf('"');
			if (beg < 0 || end <= beg)
				throw new EvaluationException(String.Format("Invalid #{0} syntax. Expected: '#{0} \"<value>\"' but '{1}' given.", derictiveName, line));

			return line.Substring(beg + 1, end - beg - 1);
		}
	}

	internal class FSharpFscEvaluator : IScriptEvaluator
	{
		public void Evaluate(FileItem script)
		{
			EmbeddedFsxCompiler.Cleanup();

			var compiledScript = EmbeddedFsxCompiler.Compile(script);

			AnFakeException.ScriptSource = new ScriptSourceInfo(
				script.Name, 
				compiledScript.Source.Name, 
				compiledScript.LinesOffset);

			compiledScript.Evaluate();		
		}
	}
}