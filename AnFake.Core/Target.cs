using System;
using System.Collections.Generic;
using System.Diagnostics;
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

		public static string Current { get; private set; }

		private readonly TraceMessageCollector _messages = new TraceMessageCollector();
		private readonly ISet<Target> _dependencies = new HashSet<Target>();
		private readonly string _name;
		private Action _do;
		private Action<ExecutionReason> _onFailure;
		private Action<ExecutionReason> _finally;
		private TargetState _state;
		private bool _skipErrors;

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

			Logger.Debug("");
			Logger.DebugFormat("'{0}' execution order:\n  {1}\n", _name, String.Join("\n  ", orderedTargets.Select(x => x.Name)));

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
			LogSummary(orderedTargets.Take(lastExecutedTarget + 1));

			// Re-throw
			if (error != null)
				throw error;
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

			if (_onFailure != null)
			{
				Invoke("OnFailure", () => _onFailure(reason), true);
			}			
		}

		private void DoFinally(ExecutionReason reason)
		{
			if (_state != TargetState.PartiallySucceeded && _state != TargetState.Failed)
				throw new InvalidOperationException(String.Format("Inconsistence in build order: trying to run Finally action for non-executed target '{0}'.", _name));

			if (_finally != null)
			{
				if (!Invoke("Finally", () => _finally(reason), true))
					return;				
			}

			if (_state == TargetState.PartiallySucceeded)
			{
				_state = TargetState.Succeeded;
			}
		}

		private void LogSummary(IEnumerable<Target> executedTargets)
		{
			var finalState = TargetState.Succeeded;
			var index = 0;

			var caption = String.Format("================ '{0}' Summary ================", _name);
			Logger.Debug("");
			Logger.Debug(caption);

			foreach (var target in executedTargets)
			{
				if (target._do == null)
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
						finalState = TargetState.PartiallySucceeded;
						break;
					case TargetState.Failed:
						Logger.Error(targetSummary);
						finalState = TargetState.PartiallySucceeded;
						break;
				}

				foreach (var message in target._messages)
				{
					switch (message.Level)
					{
						case TraceMessageLevel.Error:
							Logger.ErrorFormat("[{0,4}] {1}", ++index, message.Message);
							break;
						case TraceMessageLevel.Warning:
							Logger.WarnFormat("[{0,4}] {1}", ++index, message.Message);
							break;
						default:
							Logger.InfoFormat("[{0,4}] {1}", ++index, message.Message);
							break;
					}
				}
			}

			if (_state == TargetState.Queued || _state == TargetState.Failed)
			{
				finalState = TargetState.Failed;
			}

			Logger.Debug(new String('-', caption.Length));
			switch (finalState)
			{
				case TargetState.Succeeded:
					Logger.InfoFormat("'{0}' Succeeded", _name);
					break;
				case TargetState.PartiallySucceeded:
					Logger.WarnFormat("'{0}' Partially Succeeded", _name);
					break;
				case TargetState.Failed:
					Logger.ErrorFormat("'{0}' Failed", _name);
					break;
			}
			Logger.Debug("");
		}

		private bool Invoke(string phase, Action action, bool skipErrors)
		{
			if (action == null)
				return true;

			var setTarget = new EventHandler<TraceMessage>((s, m) => m.Target = Name);

			Current = _name;

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
				var location = "";

				if (e is TargetFailureException || e is ArgumentException)
				{					
					var frames = new StackTrace(e, true).GetFrames();
					if (frames != null)
					{
						var scriptName = MyBuild.Defaults.Script.Name;
						var scriptFrame = frames.FirstOrDefault(f =>
						{
							var file = f.GetFileName();
							return file != null && file.EndsWith(scriptName, StringComparison.InvariantCultureIgnoreCase);
						});
						if (scriptFrame != null)
						{
							location = String.Format(" @@ {0} {1}", scriptName, scriptFrame.GetFileLineNumber());
						}						
					}

					Logger.ErrorFormat("{0}{1}", e.Message, location);
				}
				else
				{
					Logger.ErrorFormat("", e, _name, phase);					
				}
				
				Tracer.ErrorFormat("{0}{1}", e, e.Message, location);

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

				Current = null;
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