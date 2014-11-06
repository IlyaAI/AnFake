using System;
using System.Collections.Generic;
using System.Linq;
using AnFake.Api;

namespace AnFake.Core
{
	public sealed class Target
	{		
		private static readonly IDictionary<string, Target> Targets = new Dictionary<string, Target>();

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

		private readonly TraceMessageCollector _messages = new TraceMessageCollector();
		private readonly ISet<Target> _dependencies = new HashSet<Target>();
		private readonly string _name;
		private Action _do;
		private Action<ExecutionReason> _onFailure;
		private Action<ExecutionReason> _finally;
		private TargetState _state;

		private Target(string name)
		{
			_name = name;

			if (Targets.ContainsKey(name))
				throw new InvalidOperationException(String.Format("Target '{0}' already exists.", name));

			Targets.Add(name, this);
		}

		public string Name
		{
			get { return _name; }
		}

		public IEnumerable<Target> Dependencies
		{
			get { return _dependencies; }
		}

		public bool HasBody
		{
			get { return _do != null; }
		}

		public Target Do(Action action)
		{
			if (action == null)
				throw new ArgumentNullException("action");

			if (_do != null)
				throw new InvalidOperationException();

			_do = action;

			return this;
		}

		public Target OnFailure(Action<ExecutionReason> action)
		{
			if (action == null)
				throw new ArgumentNullException("action");

			if (_onFailure != null)
				throw new InvalidOperationException();

			_onFailure = action;

			return this;
		}

		public Target OnFailure(Action action)
		{
			if (action == null)
				throw new ArgumentNullException("action");

			return OnFailure(x => action.Invoke());
		}

		public Target Finally(Action<ExecutionReason> action)
		{
			if (action == null)
				throw new ArgumentNullException("action");

			if (_finally != null)
				throw new InvalidOperationException();

			_finally = action;

			return this;
		}

		public Target Finally(Action action)
		{
			if (action == null)
				throw new ArgumentNullException("action");

			return Finally(x => action.Invoke());
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
			var orderedTargets = new List<Target>();
			ResolveDependencies(orderedTargets);

			Logger.Debug("Targets execution order:");			
			foreach (var target in orderedTargets)
			{
				Logger.DebugFormat("  {0}", target.Name);
			}
			Logger.Debug("");

			var lastExecutedTarget = -1;
			var lastError = (Exception) null;
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
				lastError = e;
			}

			for (var i = lastExecutedTarget; i >= 0; i--)
			{
				if (lastError != null)
				{
					orderedTargets[i].DoOnFailure(new ExecutionReason(true, i == lastExecutedTarget));
				}

				orderedTargets[i].DoFinally(new ExecutionReason(lastError != null, i == lastExecutedTarget));
			}			

			LogSummary(orderedTargets.Take(lastExecutedTarget + 1));

			if (lastError != null && !(lastError is TerminateTargetException))
				throw lastError;
		}

		private void ResolveDependencies(ICollection<Target> orderedTargets)
		{
			if (_state == TargetState.PreQueued)			
				throw new InvalidOperationException(String.Format("Target '{0}' has cycle dependency.", _name));

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
			
			_state = TargetState.Started;
			try
			{
				if (_do != null)
				{
					Logger.DebugFormat("TARGET DO >> {0}", _name);

					Invoke("Do", _do, false);
				}				

				_state = TargetState.PartiallySucceeded;
			}
			catch (Exception)
			{				
				_state = TargetState.Failed;
				throw;
			}			
		}

		private void DoOnFailure(ExecutionReason reason)
		{
			if (_state != TargetState.Failed && _state != TargetState.Succeeded && _state != TargetState.PartiallySucceeded)
				throw new InvalidOperationException(String.Format("Inconsistence in build order: trying to run OnFailure action for non-failed or non-executed target '{0}'.", _name));

			if (_onFailure != null)
			{
				Logger.DebugFormat("TARGET ON-FAILURE >> {0}", _name);

				Invoke("OnFailure", () => _onFailure(reason), true);
			}			
		}

		private void DoFinally(ExecutionReason reason)
		{
			if (_state != TargetState.PartiallySucceeded && _state != TargetState.Failed)
				throw new InvalidOperationException(String.Format("Inconsistence in build order: trying to run Finally action for non-executed target '{0}'.", _name));

			if (_finally != null)
			{
				Logger.DebugFormat("TARGET FINALLY >> {0}", _name);

				if (!Invoke("Finally", () => _finally(reason), true))
					return;				
			}

			if (_state == TargetState.PartiallySucceeded)
			{
				_state = TargetState.Succeeded;
			}
		}

		private static void LogSummary(IEnumerable<Target> executedTargets)
		{
			Logger.Debug("");
			Logger.Debug("================ BUILD SYMMARY ================");
			foreach (var target in executedTargets)
			{
				if (!target.HasBody)
					continue;

				var targetSummary = String.Format("{0}: {1} error(s) {2} warning(s)",
					target._name, target._messages.ErrorsCount, target._messages.WarningsCount);

				switch (target._state)
				{
					case TargetState.Succeeded:
						Logger.Info(targetSummary);
						break;
					case TargetState.PartiallySucceeded:
						Logger.Warn(targetSummary);
						break;
					case TargetState.Failed:
						Logger.Error(targetSummary);
						break;
				}

				foreach (var message in target._messages)
				{
					switch (message.Level)
					{
						case TraceMessageLevel.Error:
							Logger.ErrorFormat("  {0}", message.Message);
							break;
						case TraceMessageLevel.Warning:
							Logger.WarnFormat("  {0}", message.Message);
							break;
						default:
							Logger.InfoFormat("  {0}", message.Message);
							break;
					}
				}
			}
		}

		private bool Invoke(string phase, Action action, bool skipErrors)
		{
			if (action == null)
				return true;

			var setTarget = new EventHandler<TraceMessage>((s, m) => m.Target = Name);

			Tracer.MessageReceiving += setTarget;
			Tracer.MessageReceived += _messages.OnMessage;
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
				Logger.ErrorFormat("{0}.{1} has failed.", e, _name, phase);
				Tracer.Write(new TraceMessage(TraceMessageLevel.Error, e.Message) { Details = e.StackTrace });

				if (!skipErrors)
					throw new TerminateTargetException(String.Format("Target terminated due to errors in {0}.{1}", _name, phase), e);

				return false;
			}
			finally
			{
				Tracer.MessageReceived -= _messages.OnMessage;
				Tracer.MessageReceiving -= setTarget;
			}

			return true;
		}

		internal static Target Get(string name)
		{
			Target target;
			if (!Targets.TryGetValue(name, out target))
				throw new InvalidOperationException(String.Format("Target '{0}' not defined.", name));

			return target;
		}

		internal static Target Create(string name)
		{
			if (Targets.ContainsKey(name))
				throw new InvalidOperationException(String.Format("Target '{0}' already defined.", name));

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