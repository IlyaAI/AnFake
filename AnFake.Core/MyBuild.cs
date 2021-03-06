﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using AnFake.Api;
using AnFake.Core.Exceptions;

namespace AnFake.Core
{
	/// <summary>
	///		Represents set of parameters, events and tool related to entire build.
	/// </summary>
	public static class MyBuild
	{
		/// <summary>
		///     Reprsents set of build parameters.
		/// </summary>
		public sealed class Params
		{
			/// <summary>
			///		Properties passed either via command line or settings file.
			/// </summary>
			public readonly IDictionary<string, string> Properties;
			
			/// <summary>
			///		Working directory where AnFake was started.
			/// </summary>
			public readonly FileSystemPath Path;
			
			/// <summary>
			///		Log file.
			/// </summary>		
			public readonly FileItem LogFile;
			
			/// <summary>
			///		Script file.
			/// </summary>
			public readonly FileItem ScriptFile;

			/// <summary>
			///		Requested targets.
			/// </summary>
			public readonly string[] Targets;

			/// <summary>
			///		Build log verbosity (in MsBuild terms).
			/// </summary>
			public readonly Verbosity Verbosity;
			
			/// <summary>
			///		Current AnFake version.
			/// </summary>
			public readonly Version AnFakeVersion;			

			/// <summary>
			///		Local build temp folder. Default '.tmp'.
			/// </summary>
			public FileSystemPath LocalTemp;

			internal Params(FileSystemPath path, FileItem logFile, FileItem scriptFile,
				Verbosity verbosity, string[] targets, IDictionary<string, string> properties)
			{
				Path = path;
				LogFile = logFile;
				ScriptFile = scriptFile;
				Targets = targets;
				Properties = properties;
				Verbosity = verbosity;

				AnFakeVersion = typeof (MyBuild).Assembly
					.GetCustomAttribute<AssemblyFileVersionAttribute>()
					.Version
					.AsVersion();

				LocalTemp = ".tmp".AsPath();
			}
		}

		/// <summary>
		///		Represents whole build status.
		/// </summary>
		public enum Status
		{
			/* Order is important. Do not change. */
			InProgress,
			Succeeded,
			PartiallySucceeded,
			Failed,
			Unknown
		}

		/// <summary>
		///     Represents build run details. Used in <c>Started</c> and <c>Finished</c> events.
		/// </summary>
		public sealed class RunDetails
		{
			public readonly DateTime StartTime;
			public readonly DateTime FinishTime;
			public readonly Target[] ExecutedTargets;
			public readonly Status Status;

			internal RunDetails(DateTime startTime, DateTime finishTime, Target[] executedTargets, Status status)
			{
				StartTime = startTime;
				FinishTime = finishTime;
				ExecutedTargets = executedTargets;
				Status = status;
			}

			internal RunDetails(DateTime startTime)
			{
				StartTime = startTime;
				ExecutedTargets = new Target[0];
				Status = Status.InProgress;				
			}
		}		

		/// <summary>
		///     Current build parameters.
		/// </summary>
		public static Params Current { get; private set; }

		private static bool _isInitialized;
		private static EventHandler<Params> _initialized;		

		internal static void Initialize(
			FileSystemPath path, 
			FileItem logFile, 
			FileItem scriptFile,
			Verbosity verbosity, 
			string[] targets, 
			IDictionary<string, string> properties)
		{
			if (_isInitialized)
				throw new InvalidConfigurationException("MyBuild already initialized.");

			Debug.Assert(Path.IsPathRooted(logFile.Path.Spec), "LogFile must have absolute path.");
			Debug.Assert(Path.IsPathRooted(scriptFile.Path.Spec), "ScriptFile must have absolute path.");

			Current = new Params(path, logFile, scriptFile, verbosity, targets, properties);

			_isInitialized = true;
			if (_initialized != null)
			{
				_initialized.Invoke(null, Current);
			}
		}

		internal static void Finalise()
		{
			if (!_isInitialized)
				return;

			Target.Finalise();
			Plugin.Finalise();

			Current = null;
			Started = null;
			Finished = null;
			
			_initialized = null;
			_isInitialized = false;			
		}

		/// <summary>
		///     Fired when build is initialized.
		/// </summary>
		/// <remarks>
		///     Subscribing after initialization leads to immediate handler invokation.
		/// </remarks>
		public static event EventHandler<Params> Initialized
		{
			add
			{
				_initialized += value;
				if (_isInitialized)
				{
					value.Invoke(null, Current);
				}
			}
			remove { throw new NotSupportedException("It is impossible to unsubscribe from Initialized event."); }
		}		

		/// <summary>
		///     Fired when build started (just after evaluating configuration script).
		/// </summary>
		/// <remarks>
		///     Handler should not throw exceptions.
		/// </remarks>
		public static event EventHandler<RunDetails> Started;

		/// <summary>
		///     Fired when build finished either success or failure.
		/// </summary>
		/// <remarks>
		///     Handler should not throw exceptions.
		/// </remarks>
		public static event EventHandler<RunDetails> Finished;

		/// <summary>
		///     Used internally to start build.
		/// </summary>
		/// <returns>true if all requested targets completed without exception</returns>
		internal static Status Run()
		{
			var executedTargets = new List<Target>();
			var status = Status.InProgress;

			var startTime = DateTime.UtcNow;
			if (Started != null)
			{
				Started.Invoke(null, new RunDetails(startTime));
			}			

			try
			{				
				Api.Trace.Info("Validating targets...");
				// intentionally make an array to validate all targets are exists
				var requestedTargets = Current.Targets
					.Select(Target.Get)
					.ToArray();

				Api.Trace.Info("Running targets...");
				foreach (var target in requestedTargets)
				{
					executedTargets.Add(target);
					target.Run();
				}
			}
			catch (TerminateTargetException)
			{
				// no actions needed, its already processed
			}
			catch (Exception e)
			{
				status = Status.Failed;
				
				// do the best efforts to notify observers via Tracer				
				SafeOp.Try(Api.Trace.Error, AnFakeException.Wrap(e));
			}

			status = GetFinalStatus(status, executedTargets);

			var finishTime = DateTime.UtcNow;
			if (Finished != null)
			{
				Finished.Invoke(null, new RunDetails(startTime, finishTime, executedTargets.ToArray(), status));
			}

			Api.Trace.Info("Cleaning after build...");
			if (Current.LocalTemp.AsFolder().Exists())
			{
				SafeOp.Try(Folders.Delete, Current.LocalTemp);
			}
			Api.Trace.Info("Done.");

			return status;
		}

		private static Status GetFinalStatus(Status status, IEnumerable<Target> executedTargets)
		{
			foreach (var target in executedTargets)
			{
				switch (target.State)
				{
					case TargetState.Succeeded:
						if (status < Status.Succeeded)
							status = Status.Succeeded;
						break;

					case TargetState.PartiallySucceeded:
						if (status < Status.PartiallySucceeded)
							status = Status.PartiallySucceeded;
						break;

					case TargetState.Failed:
						if (status < Status.Failed)
							status = Status.Failed;
						break;
				}
			}

			return status != Status.InProgress
				? status
				: Status.Unknown;
		}

		/// <summary>
		///     Returns true if build property 'name' is specified and has non-empty value.
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		public static bool HasProp(string name)
		{
			if (String.IsNullOrEmpty(name))
				throw new ArgumentException("MyBuild.HasProp(name): name must not be null or empty");

			string value;
			return Current.Properties.TryGetValue(name, out value) && !String.IsNullOrEmpty(value);
		}

		/// <summary>
		///     Returns non empty value of build property 'name'. If no such property specified or it has empty value then
		///     exception is thrown.
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		public static string GetProp(string name)
		{
			if (String.IsNullOrEmpty(name))
				throw new ArgumentException("MyBuild.GetProp(name): name must not be null or empty");

			string value;
			if (!Current.Properties.TryGetValue(name, out value))
				throw new InvalidConfigurationException(String.Format("Build property '{0}' is not specified.", name));
			if (String.IsNullOrEmpty(value))
				throw new InvalidConfigurationException(String.Format("Build property '{0}' has empty value.", name));

			return value;
		}

		/// <summary>
		///     Returns non empty value of build property 'name'. If no such property specified or it has empty value then
		///     'defaultValue' is returned.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="defaultValue"></param>
		/// <returns></returns>
		public static string GetProp(string name, string defaultValue)
		{
			if (String.IsNullOrEmpty(name))
				throw new ArgumentException("MyBuild.GetProp(name, defaultValue): name must not be null or empty");

			string value;
			return Current.Properties.TryGetValue(name, out value) && !String.IsNullOrEmpty(value)
				? value
				: defaultValue;
		}

		/// <summary>
		///     Sets new value of build property 'name'.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="value"></param>
		public static void SetProp(string name, string value)
		{
			if (String.IsNullOrEmpty(name))
				throw new ArgumentException("MyBuild.SetProp(name, value): name must not be null or empty");
			if (value == null)
				throw new ArgumentException("MyBuild.SetProp(name, value): value must not be null");

			Current.Properties[name] = value;
		}

		/// <summary>
		///     Saves specified properties to settings file.
		/// </summary>
		/// <remarks>
		///     Properties are saved to settings file in Application Data folder for current user.
		///     These settings will be automatically loaded on next run.
		/// </remarks>
		/// <param name="names"></param>
		public static void SaveProp(params string[] names)
		{
			if (names.Length == 0)
				throw new ArgumentException("MyBuild.SaveProp(name[, ...]): at least one name should be specified");

			if (names.Any(String.IsNullOrEmpty))
				throw new ArgumentException("MyBuild.SaveProp(name[, ...]): name must not be null or empty");

			foreach (var name in names)
			{
				string value;
				if (Current.Properties.TryGetValue(name, out value))
				{
					Settings.Current.Set(name, value);
				}
				else
				{
					Settings.Current.Remove(name);
				}
			}

			Settings.Current.Save();
		}

		/// <summary>
		///     Throws an exception indicating custom build failure.
		/// </summary>
		/// <param name="format"></param>
		/// <param name="args"></param>
		public static void Failed(string format, params object[] args)
		{
			if (String.IsNullOrEmpty(format))
				throw new ArgumentException("MyBuild.Failed(format): format must not be null or empty");

			throw new TargetFailureException(String.Format(format, args));
		}
	}
}