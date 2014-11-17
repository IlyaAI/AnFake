using System;
using System.Collections.Generic;

namespace AnFake.Core.Utils
{
	public sealed class DependencyResolver<T>
	{
		private readonly Func<T, IEnumerable<T>> _getDependencies;

		public DependencyResolver(Func<T, IEnumerable<T>> getDependencies)
		{
			_getDependencies = getDependencies;
		}

		public IList<T> Resolve(IEnumerable<T> items)
		{
			var resolving = new HashSet<T>();
			var resolved = new HashSet<T>();
			var ordered = new List<T>();

			foreach (var item in items)
			{
				DoResolve(item, resolving, resolved, ordered);
			}

			return ordered;
		}

		public IList<T> Resolve(T item)
		{
			return Resolve(new[] {item});
		}

		private void DoResolve(T current, ISet<T> resolving, ISet<T> resolved, IList<T> ordered)
		{
			if (resolving.Contains(current))
				throw new CycleDependencyException();

			if (resolved.Contains(current))
				return;

			resolving.Add(current);
			foreach (var dependent in _getDependencies(current))
			{
				DoResolve(dependent, resolving, resolved, ordered);
			}

			ordered.Add(current);			
			resolved.Add(current);
			resolving.Remove(current);
		}
	}
}