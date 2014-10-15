using System;
using System.Collections.Generic;
using AnFake.Api;
using Common.Logging;

namespace AnFake.Core
{
	public sealed class Target
	{
		private static readonly ILog Log = LogManager.GetLogger<Target>();
		private static readonly IDictionary<string, Target> Targets = new Dictionary<string, Target>();

		private readonly TraceMessageCollector _messages = new TraceMessageCollector();
		private readonly ISet<Target> _dependencies = new HashSet<Target>();
		private readonly string _name;
		private Action _do;
		private Action _onFailure;
		private Action _finally;
		private TargetState _state;

		private Target(string name)
		{
			_name = name;

			if (Targets.ContainsKey(name))
				throw new InvalidOperationException();

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

		public Target OnFailure(Action action)
		{
			if (action == null)
				throw new ArgumentNullException("action");

			if (_onFailure != null)
				throw new InvalidOperationException();

			_onFailure = action;

			return this;
		}

		public Target Finally(Action action)
		{
			if (action == null)
				throw new ArgumentNullException("action");

			if (_finally != null)
				throw new InvalidOperationException();

			_finally = action;

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
		
		public void Run()
		{
			Log.Info("Targets execution order:");
			DoValidate(1);
			Log.Info("");

			var executedTargets = new List<Target>();
			try
			{
				DoRun(executedTargets);
			}
			catch (TerminateTargetException)
			{
				// do nothing
			}

			LogSummary(executedTargets);
		}

		private void DoValidate(int depth)
		{
			var ident = new String(' ', depth * 2);			

			if (_state == TargetState.PreQueued)
			{
				Log.ErrorFormat("{0}{1} => CYCLING DEPENDENCY DETECTED", ident, _name);

				throw new InvalidOperationException(String.Format("Target '{0}' has cycling dependencies.", _name));
			}

			if (_state == TargetState.Queued)
				return;
			
			_state = TargetState.PreQueued;			
			Log.InfoFormat("{0}{1} =>", ident, _name);
			if (_onFailure != null || _finally != null)
			{
				Log.InfoFormat("{0}TRY", ident);				
			}			

			foreach (var dependency in _dependencies)
			{
				dependency.DoValidate(depth + 1);
			}

			if (_do != null)
			{
				Log.InfoFormat("{0}  {1}.Do", ident, _name);
			}

			if (_onFailure != null)
			{
				Log.InfoFormat("{0}CATCH", ident);
				Log.InfoFormat("{0}  {1}.OnFailure", ident, _name);
			}

			if (_finally != null)
			{
				Log.InfoFormat("{0}FINALLY", ident);
				Log.InfoFormat("{0}  {1}.Finally", ident, _name);
			}			
				
			_state = TargetState.Queued;			
		}		

		private void DoRun(ICollection<Target> executedTargets)
		{
			if (_state == TargetState.Succeeded || _state == TargetState.PartiallySucceeded)
				return;

			if (_state == TargetState.Failed)
				throw new InvalidOperationException(String.Format("Inconsistence in build oreder: trying to re-run failed target '{0}'.", _name));

			Log.InfoFormat("TARGET START >> {0}", _name);

			_state = TargetState.Started;
			try
			{
				foreach (var dependency in _dependencies)
				{
					dependency.DoRun(executedTargets);					
				}

				executedTargets.Add(this);
				Invoke("Do", _do, false);

				_state = TargetState.PartiallySucceeded;
			}
			catch (Exception)
			{
				Invoke("OnFailure", _onFailure, true);
				_state = TargetState.Failed;

				throw;
			}
			finally
			{
				if (Invoke("Finally", _finally, true))
				{
					if (_state == TargetState.PartiallySucceeded)
					{
						_state = TargetState.Succeeded;
					}					
				}

				Log.InfoFormat("TARGET END   >> {0}", _name);
			}
		}

		private static void LogSummary(IEnumerable<Target> executedTargets)
		{
			Log.Info("");
			Log.Info("================ BUILD SYMMARY ================");
			foreach (var target in executedTargets)
			{
				var targetSummary = String.Format("=> {0}: {1} error(s) {2} warning(s)",
					target._name, target._messages.ErrorsCount, target._messages.WarningsCount);

				switch (target._state)
				{
					case TargetState.Succeeded:
						Log.Info(targetSummary);
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
				Log.ErrorFormat("{0}.{1} has failed.", e, _name, phase);
				Tracer.Write(new TraceMessage(TraceMessageLevel.Error, e.Message) { Details = e.StackTrace });

				if (!skipErrors)
					throw new TerminateTargetException(String.Format("Target terminated due to errors in {0}.{1}", _name, phase), e);

				return false;
			}
			finally
			{
				Tracer.MessageReceived -= _messages.OnMessage;
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