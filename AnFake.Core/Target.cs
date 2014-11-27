using System;
using System.Collections.Generic;
using System.Linq;
using AnFake.Api;
using AnFake.Core.Exceptions;

namespace AnFake.Core
{
	public sealed class Target
	{
		private static readonly IDictionary<string, Target> Targets = new Dictionary<string, Target>(StringComparer.OrdinalIgnoreCase);

		public sealed class ExecutionReason
		{
			public readonly bool IsRunFailed;
			public readonly bool IsTargetFailed;

			internal ExecutionReason(bool isRunFailed, bool isTargetFailed)
			{
				IsRunFailed = isRunFailed;
				IsTargetFailed = isTargetFailed;
			}
		}

		public sealed class RunFinishedEventArgs : EventArgs
		{
			public readonly TargetState FinalState;
			public readonly Target[] ExecutedTargets;

			public RunFinishedEventArgs(TargetState finalState, Target[] executedTargets)
			{
				FinalState = finalState;
				ExecutedTargets = executedTargets;
			}
		}

		private static Target _current;

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
		private event EventHandler<ExecutionReason> Failed;
		private event EventHandler<ExecutionReason> Finalized;

		private Target(string name)
		{
			_name = name;

			if (Targets.ContainsKey(name))
				throw new InvalidConfigurationException(String.Format("Target '{0}' already defined.", name));

			Targets.Add(name, this);
		}

		public static event EventHandler<RunFinishedEventArgs> RunFinished;

		public static event EventHandler<ExecutionReason> CurrentFailed
		{
			add { Current.Failed += value; }
			remove { Current.Failed -= value; }
		}

		public static event EventHandler<ExecutionReason> CurrentFinalized
		{
			add { Current.Finalized += value; }
			remove { Current.Finalized -= value; }
		}

		public string Name
		{
			get { return _name; }
		}

		public IEnumerable<Target> Dependencies
		{
			get { return _dependencies; }
		}

		public TargetState State
		{
			get { return _state; }
		}

		public bool HasBody
		{
			get { return _do != null; }
		}

		public TraceMessageCollector Messages
		{
			get { return _messages; }
		}		

		public Target Do(Action action)
		{
			if (action == null)
				throw new AnFakeArgumentException("Target.Do(action): action must not be null");

			if (_do != null)
				throw new InvalidConfigurationException(String.Format("Target '{0}' already has a body.", _name));

			_do = action;

			return this;
		}

		public Target OnFailure(Action<ExecutionReason> action)
		{
			if (action == null)
				throw new AnFakeArgumentException("Target.OnFailure(action): action must not be null");

			if (_onFailure != null)
				throw new InvalidConfigurationException(String.Format("Target '{0}' already has on-failure handler.", _name));

			_onFailure = action;			

			return this;
		}

		public Target OnFailure(Action action)
		{
			if (action == null)
				throw new AnFakeArgumentException("Target.OnFailure(action): action must not be null");

			return OnFailure(x => action.Invoke());
		}

		public Target Finally(Action<ExecutionReason> action)
		{
			if (action == null)
				throw new AnFakeArgumentException("Target.Finally(action): action must not be null");

			if (_finally != null)
				throw new InvalidConfigurationException(String.Format("Target '{0}' already has finally handler.", _name));

			_finally = action;

			return this;
		}

		public Target Finally(Action action)
		{
			if (action == null)
				throw new AnFakeArgumentException("Target.Finally(action): action must not be null");

			return Finally(x => action.Invoke());
		}

		public Target SkipErrors()
		{
			_skipErrors = true;

			return this;
		}

		public Target DependsOn(params string[] names)
		{
			return DependsOn((IEnumerable<string>)names);
		}

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

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			if (ReferenceEquals(this, obj)) return true;
			return obj is Target && Equals((Target) obj);
		}

		public override int GetHashCode()
		{
			return _name.GetHashCode();
		}
		
		internal void Run()
		{
			// Prepare
			var orderedTargets = new List<Target>();
			ResolveDependencies(orderedTargets);
			
			Logger.DebugFormat("'{0}' execution order:\n  {1}", _name, String.Join("\n  ", orderedTargets.Select(x => x.Name)));

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
				if (!reason.IsRunFailed && !reason.IsTargetFailed)
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
			var finalState = GetFinalState(executedTargets);

			LogSummary(finalState, executedTargets);

			if (RunFinished != null)
			{
				RunFinished.Invoke(this, new RunFinishedEventArgs(finalState, executedTargets));
			}

			// Re-throw
			if (error != null)
				throw error;
		}

		private void ResolveDependencies(ICollection<Target> orderedTargets)
		{
			if (_state == TargetState.PreQueued)			
				throw new InvalidConfigurationException(String.Format("Target '{0}' has cycle dependency.", _name));

			if (_state == TargetState.Queued)
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

			_state = TargetState.Started;
			try
			{
				if (_do != null)
				{
					Invoke("Do", _do, false);
				}				

				_state = TargetState.PartiallySucceeded;
			}
			catch (Exception)
			{				
				_state = TargetState.Failed;
				if (!_skipErrors)
					throw;
			}			
		}

		private void DoOnFailure(ExecutionReason reason)
		{
			if (_state != TargetState.Failed && _state != TargetState.Succeeded && _state != TargetState.PartiallySucceeded)
				throw new InvalidOperationException(String.Format("Inconsistence in build order: trying to run OnFailure action for non-failed or non-executed target '{0}'.", _name));

			if (_onFailure != null || Failed != null)
			{
				Invoke(
					"OnFailure",
					() =>
					{
						if (_onFailure != null)
							_onFailure.Invoke(reason);

						if (Failed != null)
							Failed.Invoke(this, reason);
					},
					true);
			}
		}

		private void DoFinally(ExecutionReason reason)
		{
			if (_state != TargetState.PartiallySucceeded && _state != TargetState.Failed)
				throw new InvalidOperationException(String.Format("Inconsistence in build order: trying to run Finally action for non-executed target '{0}'.", _name));

			if (_finally != null || Finalized != null)
			{
				var ret = Invoke(
					"Finally",
					() =>
					{
						if (_finally != null)
							_finally.Invoke(reason);

						if (Finalized != null)
							Finalized.Invoke(this, reason);
					},
					true);

				if (!ret)
					return;				
			}

			if (_state == TargetState.PartiallySucceeded)
			{
				_state = TargetState.Succeeded;
			}
		}

		private TargetState GetFinalState(IEnumerable<Target> executedTargets)
		{
			if (_state == TargetState.Queued || _state == TargetState.Failed)
				return TargetState.Failed;

			return executedTargets.Any(x => x.State != TargetState.Succeeded)
				? TargetState.PartiallySucceeded
				: TargetState.Succeeded;			
		}

		private void LogSummary(TargetState finalState, IEnumerable<Target> executedTargets)
		{			
			var index = 0;

			var caption = String.Format("================ '{0}' Summary ================", _name);
			Logger.Debug("");
			Logger.Debug(caption);

			foreach (var target in executedTargets)
			{
				Logger.TargetStateFormat(target.State, "{0}: {1} error(s) {2} warning(s) {3} message(s)", 
					target.Name, target.Messages.ErrorsCount, target.Messages.WarningsCount, target.Messages.SummariesCount);
				
				foreach (var message in target.Messages)
				{
					Logger.TraceMessageFormat(message.Level, "[{0,4}] {1}", ++index, message.ToString());
				}
			}

			Logger.Debug(new String('-', caption.Length));
			Logger.TargetStateFormat(finalState, "'{0}' {1}", _name, finalState.ToHumanReadable());
			Logger.Debug("");
		}

		private bool Invoke(string phase, Action action, bool skipErrors)
		{
			if (action == null)
				return true;

			var setTarget = new EventHandler<TraceMessage>((s, m) => m.Target = Name);

			_current = this;

			Logger.Debug("");
			Logger.DebugFormat("START {0}.{1}", _name, phase);
			
			Tracer.MessageReceiving += setTarget;
			Tracer.MessageReceived += _messages.OnMessage;
			Tracer.InfoFormat("START {0}.{1}", _name, phase);
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
				var error = (e as AnFakeException) ?? new AnFakeWrapperException(e);

				Logger.Error(error);
				Tracer.Error(error);

				if (!skipErrors)
					throw new TerminateTargetException(String.Format("Target terminated due to errors in {0}.{1}", _name, phase), e);

				return false;
			}
			finally
			{
				Tracer.InfoFormat("END   {0}.{1}", _name, phase);
				Tracer.MessageReceived -= _messages.OnMessage;
				Tracer.MessageReceiving -= setTarget;				

				Logger.DebugFormat("END   {0}.{1}", _name, phase);

				_current = null;
			}

			return true;
		}

		internal static Target Get(string name)
		{
			Target target;
			if (!Targets.TryGetValue(name, out target))
				throw new InvalidConfigurationException(String.Format("Target '{0}' not defined. Available targets are: {1}.", name, String.Join(", ", Targets.Keys)));

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
		
		internal static void Reset()
		{
			Targets.Clear();
		}
	}
}