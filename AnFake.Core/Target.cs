using System;
using System.Collections.Generic;
using System.Linq;
using AnFake.Api;
using AnFake.Core.Exceptions;

namespace AnFake.Core
{
	/// <summary>
	///     Represents a build target.
	/// </summary>
	/// <remarks>
	///     Target is instantiated by an extension method AsTarget or by operator => in F#.
	/// </remarks>
	public sealed class Target
	{
		public const string DoPhase = "Do";
		public const string OnFailurePhase = "OnFailure";
		public const string FinallyPhase = "Finally";

		private static readonly IDictionary<string, Target> Targets
			= new Dictionary<string, Target>(StringComparer.OrdinalIgnoreCase);

		/// <summary>
		///     Represents execution reason of on-failure or finally action.
		/// </summary>
		public sealed class ExecutionReason
		{
			/// <summary>
			///     Whether top requested target failed or not.
			/// </summary>
			public readonly bool IsTopFailed;

			/// <summary>
			///     Whether current target failed or not.
			/// </summary>
			public readonly bool IsCurrentFailed;

			internal ExecutionReason(bool isTopFailed, bool isCurrentFailed)
			{
				IsTopFailed = isTopFailed;
				IsCurrentFailed = isCurrentFailed;
			}
		}

		/// <summary>
		///     Represents target run details. Used in <c>Started</c> and <c>Finished</c> events.
		/// </summary>
		public sealed class RunDetails
		{
			/// <summary>
			///     Target start time.
			/// </summary>
			public readonly DateTime StartTime;

			/// <summary>
			///     Target finish time.
			/// </summary>
			/// <remarks>
			///     This value is undefined for <c>Finished</c> event.
			/// </remarks>
			public readonly DateTime FinishTime;

			/// <summary>
			///     Set of executed targets.
			/// </summary>
			/// <remarks>
			///     <para>This set contains all targets which Do action was invoked.</para>
			///     <para>This set is empty for <c>Finished</c> event.</para>
			/// </remarks>
			public readonly Target[] ExecutedTargets;

			internal RunDetails(DateTime startTime, DateTime finishTime, Target[] executedTargets)
			{
				StartTime = startTime;
				FinishTime = finishTime;
				ExecutedTargets = executedTargets;
			}

			internal RunDetails(DateTime startTime)
			{
				StartTime = startTime;
				ExecutedTargets = new Target[0];
			}
		}

		private static Target _current;

		/// <summary>
		///     Currently running target.
		/// </summary>
		public static Target Current
		{
			get
			{
				if (_current == null)
					throw new InvalidConfigurationException(
						"Target.Current is unavailable. Hint: this property available inside target body or on-failure/finally handlers only.");

				return _current;
			}
		}

		private readonly TraceMessageCollector _messages = new TraceMessageCollector();
		private readonly ISet<Target> _dependencies = new HashSet<Target>();
		private readonly string _name;
		private Action _do;
		private Action<ExecutionReason> _onFailure;
		private Action<ExecutionReason> _finally;
		private TargetState _state;
		private bool _skipErrors;
		private bool _failIfAnyWarning;
		private bool _partialSucceedIfAnyWarning;
		private TimeSpan _runTime = TimeSpan.Zero;
		private event EventHandler<ExecutionReason> Failed;
		private event EventHandler<ExecutionReason> Finalized;

		private Target(string name)
		{
			_name = name;

			if (Targets.ContainsKey(name))
				throw new InvalidConfigurationException(String.Format("Target '{0}' already defined.", name));

			Targets.Add(name, this);
		}

		/// <summary>
		///     Fired when top target started.
		/// </summary>
		/// <remarks>
		///     This event fired for top-level targets only (i.e. for targets requested by user).
		/// </remarks>
		public static event EventHandler<RunDetails> TopStarted;

		/// <summary>
		///     Fired when top target finished either successful or failed.
		/// </summary>
		/// <remarks>
		///     This event fired for top-level targets only (i.e. for targets requested by user).
		///     Property RunDetails.ExecutedTargets includes top-level target itself and all dependent targets in order of
		///     execution.
		/// </remarks>
		public static event EventHandler<RunDetails> TopFinished;

		/// <summary>
		///     Fired when target's phase started.
		/// </summary>
		public static event EventHandler<string> PhaseStarted;

		/// <summary>
		///     Fired when target's phase finished either successful or failed.
		/// </summary>		
		public static event EventHandler<string> PhaseFinished;		

		/// <summary>
		///     Fired when currently running target failed.
		/// </summary>
		public static event EventHandler<ExecutionReason> CurrentFailed
		{
			add { Current.Failed += value; }
			remove { Current.Failed -= value; }
		}

		/// <summary>
		///     Fired when currently running target finalized.
		/// </summary>
		public static event EventHandler<ExecutionReason> CurrentFinalized
		{
			add { Current.Finalized += value; }
			remove { Current.Finalized -= value; }
		}

		/// <summary>
		///     Target name.
		/// </summary>
		public string Name
		{
			get { return _name; }
		}

		/// <summary>
		///     Target dependencies.
		/// </summary>
		/// <remarks>
		///     Targets from Dependencies sequence are run before current.
		/// </remarks>
		public IEnumerable<Target> Dependencies
		{
			get { return _dependencies; }
		}

		/// <summary>
		///     Current target state.
		/// </summary>
		public TargetState State
		{
			get { return _state; }
		}

		/// <summary>
		///     Whether target has body (i.e. Do action) or not.
		/// </summary>
		public bool HasBody
		{
			get { return _do != null; }
		}

		/// <summary>
		///		Whether skip (ignore) errors during this target execution or not.
		/// </summary>
		public bool IsSkipErrors
		{
			get { return _skipErrors; }
		}

		/// <summary>
		///		Whether fail on any warning or not.
		/// </summary>
		public bool IsFailIfAnyWarning
		{
			get { return _failIfAnyWarning; }
		}

		/// <summary>
		///     Trace messages accumulated during target run.
		/// </summary>
		public TraceMessageCollector Messages
		{
			get { return _messages; }
		}

		/// <summary>
		///     Target run time.
		/// </summary>
		public TimeSpan RunTime
		{
			get { return _runTime; }
		}

		/// <summary>
		///     Defines target body.
		/// </summary>
		/// <param name="action"></param>
		/// <returns></returns>
		public Target Do(Action action)
		{
			if (action == null)
				throw new ArgumentException("Target.Do(action): action must not be null");

			if (_do != null)
				throw new InvalidConfigurationException(String.Format("Target '{0}' already has a body.", _name));

			_do = action;

			return this;
		}

		/// <summary>
		///     Defines target on-failure action.
		/// </summary>
		/// <param name="action"></param>
		/// <returns></returns>
		public Target OnFailure(Action<ExecutionReason> action)
		{
			if (action == null)
				throw new ArgumentException("Target.OnFailure(action): action must not be null");

			if (_onFailure != null)
				throw new InvalidConfigurationException(String.Format("Target '{0}' already has on-failure handler.", _name));

			_onFailure = action;

			return this;
		}

		/// <summary>
		///     Defines target on-failure action.
		/// </summary>
		/// <param name="action"></param>
		/// <returns></returns>
		public Target OnFailure(Action action)
		{
			if (action == null)
				throw new ArgumentException("Target.OnFailure(action): action must not be null");

			return OnFailure(x => action.Invoke());
		}

		/// <summary>
		///     Defines target finally action.
		/// </summary>
		/// <remarks>
		///     Finally action is executed eigher target successful or failed.
		/// </remarks>
		/// <param name="action"></param>
		/// <returns></returns>
		public Target Finally(Action<ExecutionReason> action)
		{
			if (action == null)
				throw new ArgumentException("Target.Finally(action): action must not be null");

			if (_finally != null)
				throw new InvalidConfigurationException(String.Format("Target '{0}' already has finally handler.", _name));

			_finally = action;

			return this;
		}

		/// <summary>
		///     Defines target finally action.
		/// </summary>
		/// <remarks>
		///     Finally action is executed eigher target successful or failed.
		/// </remarks>
		/// <param name="action"></param>
		/// <returns></returns>
		public Target Finally(Action action)
		{
			if (action == null)
				throw new ArgumentException("Target.Finally(action): action must not be null");

			return Finally(x => action.Invoke());
		}

		/// <summary>
		///     Instructs this target to skip errors, i.e. do not fail even some error occured.
		/// </summary>
		/// <remarks>
		///     Error might be reported eigther by throwing an exception or by writing error message to tracer.
		/// </remarks>
		/// <returns></returns>
		public Target SkipErrors()
		{
			_skipErrors = true;

			return this;
		}

		/// <summary>
		///     Instructs this target to fail if any warning message was written to tracer.
		/// </summary>
		/// <returns></returns>
		public Target FailIfAnyWarning()
		{
			_failIfAnyWarning = true;

			return this;
		}

		/// <summary>
		///     Instructs this target to partial succeed if any warning message was written to tracer.
		/// </summary>
		/// <returns></returns>
		public Target PartialSucceedIfAnyWarning()
		{
			_partialSucceedIfAnyWarning = true;

			return this;
		}

		/// <summary>
		///     Defines target which this one depends on.
		/// </summary>
		/// <param name="names"></param>
		/// <returns></returns>
		public Target DependsOn(params string[] names)
		{
			return DependsOn((IEnumerable<string>) names);
		}

		/// <summary>
		///     Defines target which this one depends on.
		/// </summary>
		/// <param name="names"></param>
		/// <returns></returns>
		public Target DependsOn(IEnumerable<string> names)
		{
			foreach (var name in names)
			{
				_dependencies.Add(Get(name));
			}

			return this;
		}

		private bool Equals(Target other)
		{
			return string.Equals(_name, other._name);
		}

		/// <summary>
		///     Returns true if targets have the same name.
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			if (ReferenceEquals(this, obj)) return true;
			return obj is Target && Equals((Target) obj);
		}

		/// <summary>
		///     Returns hash code based on target name.
		/// </summary>
		/// <returns></returns>
		public override int GetHashCode()
		{
			return _name.GetHashCode();
		}

		/// <summary>
		///     Used internally to run target.
		/// </summary>
		internal void Run()
		{
			if (_state == TargetState.Succeeded
				|| _state == TargetState.PartiallySucceeded
				|| _state == TargetState.Failed)
				return;

			// Prepare
			var orderedTargets = new List<Target>();
			ResolveDependencies(orderedTargets);

			Trace.InfoFormat("'{0}' execution order: {1}.", _name, String.Join(", ", orderedTargets.Select(x => x.Name)));

			var startTime = DateTime.UtcNow;
			if (TopStarted != null)
			{
				TopStarted.Invoke(this, new RunDetails(startTime));
			}

			// Target.Do
			var lastExecutedTarget = -1;
			var error = (Exception) null;
			try
			{
				for (var i = 0; i < orderedTargets.Count; i++)
				{
					lastExecutedTarget = i;
					orderedTargets[i].DoMain();
				}
			}
			catch (Exception e)
			{
				error = e;
			}

			// Target.OnFailure
			for (var i = lastExecutedTarget; i >= 0; i--)
			{
				var executedTarget = orderedTargets[i];
				var reason = new ExecutionReason(error != null, executedTarget._state == TargetState.Failed);
				if (!reason.IsTopFailed && !reason.IsCurrentFailed)
					continue;

				executedTarget.DoOnFailure(reason);
			}

			// Target.Finally
			for (var i = lastExecutedTarget; i >= 0; i--)
			{
				var executedTarget = orderedTargets[i];
				executedTarget.DoFinally(new ExecutionReason(error != null, executedTarget._state == TargetState.Failed));
			}

			// Summarize
			var executedTargets = orderedTargets
				.Take(lastExecutedTarget + 1)
				.Where(x => x.HasBody)
				.ToArray();

			FinalizeState(executedTargets);

			LogSummary(executedTargets);

			var finishTime = DateTime.UtcNow;
			if (TopFinished != null)
			{
				TopFinished.Invoke(this, new RunDetails(startTime, finishTime, executedTargets));
			}

			// Re-throw
			if (error != null)
				throw error;
		}

		private void ResolveDependencies(ICollection<Target> orderedTargets)
		{
			if (_state == TargetState.PreQueued)
				throw new InvalidConfigurationException(String.Format("Target '{0}' has cycle dependency.", _name));

			if (_state == TargetState.Queued ||
				_state == TargetState.Succeeded ||
				_state == TargetState.PartiallySucceeded ||
				_state == TargetState.Failed)
				return;

			_state = TargetState.PreQueued;
			foreach (var dependent in _dependencies)
			{
				dependent.ResolveDependencies(orderedTargets);
			}

			orderedTargets.Add(this);

			_state = TargetState.Queued;
		}

		private void DoMain()
		{
			if (_state == TargetState.Succeeded || _state == TargetState.PartiallySucceeded)
				return;

			if (_state == TargetState.Failed)
				throw new InvalidOperationException(String.Format("Inconsistence in build order: trying to re-run failed target '{0}'.", _name));

			Failed = null;
			Finalized = null;

			if (_do == null)
			{
				_state = TargetState.PartiallySucceeded;
				return;
			}
			
			_state = TargetState.Started;
			try
			{				
				if (PhaseStarted != null)
				{
					PhaseStarted.Invoke(this, DoPhase);
				}

				Invoke(DoPhase, _do, false);

				if (_failIfAnyWarning && _messages.WarningsCount > 0)
					throw new TerminateTargetException("Target terminated due to reported warning/error(s).");
				
				_state = TargetState.PartiallySucceeded;
			}
			catch (Exception)
			{
				_state = TargetState.Failed;
				if (!_skipErrors)
					throw;
			}
			finally
			{
				if (PhaseFinished != null)
				{
					SafeOp.Try(PhaseFinished.Invoke, this, DoPhase);
				}
			}
		}

		private void DoOnFailure(ExecutionReason reason)
		{
			if (_state != TargetState.Failed && _state != TargetState.Succeeded && _state != TargetState.PartiallySucceeded)
				throw new InvalidOperationException(
					String.Format("Inconsistence in build order: trying to run OnFailure action for non-failed or non-executed target '{0}'.", _name));

			if (_onFailure == null && Failed == null) 
				return;

			try
			{
				if (PhaseStarted != null)
				{
					PhaseStarted.Invoke(this, OnFailurePhase);
				}

				Invoke(
					OnFailurePhase,
					() =>
					{
						if (_onFailure != null)
							_onFailure.Invoke(reason);

						if (Failed != null)
							Failed.Invoke(this, reason);
					},
					true);
			}
			finally
			{
				if (PhaseFinished != null)
				{
					SafeOp.Try(PhaseFinished.Invoke, this, OnFailurePhase);
				}	
			}			
		}

		private void DoFinally(ExecutionReason reason)
		{
			if (_state != TargetState.PartiallySucceeded && _state != TargetState.Failed)
				throw new InvalidOperationException(String.Format("Inconsistence in build order: trying to run Finally action for non-executed target '{0}'.", _name));

			if (_finally == null && Finalized == null)
			{
				UpgradeState();
				return;
			}

			try
			{
				if (PhaseStarted != null)
				{
					PhaseStarted.Invoke(this, FinallyPhase);
				}

				var ret = Invoke(
					FinallyPhase,
					() =>
					{
						if (_finally != null)
							_finally.Invoke(reason);

						if (Finalized != null)
							Finalized.Invoke(this, reason);
					},
					true);

				if (ret)
				{
					UpgradeState();
				}					
			}
			finally
			{
				if (PhaseFinished != null)
				{
					SafeOp.Try(PhaseFinished.Invoke, this, FinallyPhase);
				}
			}
		}

		private void UpgradeState()
		{
			if (_state == TargetState.PartiallySucceeded && (!_partialSucceedIfAnyWarning || _messages.WarningsCount == 0))
			{
				_state = TargetState.Succeeded;
			}
		}

		private void FinalizeState(IEnumerable<Target> executedTargets)
		{
			if (_state == TargetState.Queued)
			{
				_state = TargetState.Failed;
				return;
			}

			if (_state == TargetState.Failed)
			{
				_state = _skipErrors
					? TargetState.PartiallySucceeded
					: TargetState.Failed;
				return;
			}

			_state = executedTargets.Any(x => x.State != TargetState.Succeeded)
				? TargetState.PartiallySucceeded
				: TargetState.Succeeded;
		}

		private void LogSummary(IEnumerable<Target> executedTargets)
		{
			var index = 0;

			var caption = String.Format("================ '{0}' Summary ================", _name);
			Log.Text("");
			Log.Text(caption);

			foreach (var target in executedTargets)
			{
				Log.Text("");
				LogEx.TargetStateFormat(target.State, "{0}: {1} error(s) {2} warning(s) {3} message(s)",
					target.Name, target.Messages.ErrorsCount, target.Messages.WarningsCount, target.Messages.SummariesCount);

				foreach (var message in target.Messages)
				{
					Log.TraceMessageFormat(message.Level, "[{0,4}] {1}", ++index, message.ToString("m"));

					var refs = message.ToString("lf");
					if (refs.Length > 0)
						Log.Text(refs);

					var more = message.ToString("d");
					if (more.Length > 0)
						Log.Details(more);
				}
			}

			Log.Text(new String('-', caption.Length));
			LogEx.TargetStateFormat(_state, "'{0}' {1}", _name, _state.ToHumanReadable());
			Log.Text("");
		}

		private bool Invoke(string phase, Action action, bool skipErrors)
		{
			if (action == null)
				return true;

			var setTarget = new EventHandler<TraceMessage>((s, m) => m.Target = Name);

			_current = this;

			var startTime = Environment.TickCount;

			Trace.InfoFormat(">>> '{0}.{1}' started.", _name, phase);

			Trace.MessageReceiving += setTarget;
			Trace.MessageReceived += _messages.OnMessage;
			try
			{
				var existingErrors = _messages.ErrorsCount;

				action.Invoke();

				if (_messages.ErrorsCount > existingErrors)
					throw new TerminateTargetException(String.Format("Target '{0}' terminated due to errors reported via tracer.", _name));
			}
			catch (TerminateTargetException)
			{
				if (!skipErrors)
					throw;

				return false;
			}
			catch (Exception e)
			{
				var error = AnFakeException.Wrap(e);

				Trace.Error(error);

				if (!skipErrors)
					throw new TerminateTargetException(String.Format("Target terminated due to errors in {0}.{1}", _name, phase), e);

				return false;
			}
			finally
			{
				Trace.InfoFormat("<<< '{0}.{1}' finished.", _name, phase);
				Trace.MessageReceived -= _messages.OnMessage;
				Trace.MessageReceiving -= setTarget;				

				var finishTime = Environment.TickCount;
				_runTime += TimeSpan.FromMilliseconds(finishTime - startTime);

				_current = null;
			}

			return true;
		}

		internal static Target Get(string name)
		{
			Target target;
			if (!Targets.TryGetValue(name, out target))
				throw new InvalidConfigurationException(String.Format("Target '{0}' not defined.\nAvailable targets are:\n  {1}", name, String.Join("\n  ", Targets.Keys)));

			return target;
		}

		internal static Target Create(string name)
		{
			if (Targets.ContainsKey(name))
				throw new InvalidConfigurationException(String.Format("Target '{0}' already defined.", name));

			return new Target(name);
		}

		internal static Target GetOrCreate(string name)
		{
			Target target;
			return Targets.TryGetValue(name, out target)
				? target
				: new Target(name);
		}

		internal static void Finalise()
		{
			Targets.Clear();
		}
	}
}