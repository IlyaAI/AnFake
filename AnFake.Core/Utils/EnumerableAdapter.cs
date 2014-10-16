using System.Collections;
using System.Collections.Generic;

namespace AnFake.Core.Utils
{
	public sealed class EnumerableAdapter<T> : IEnumerable<T>
	{
		private readonly IEnumerable _enumerable;

		private class EnumeratorAdapter : IEnumerator<T>
		{
			private readonly IEnumerator _enumerator;

			public EnumeratorAdapter(IEnumerator enumerator)
			{
				_enumerator = enumerator;
			}

			public void Dispose()
			{
			}

			public bool MoveNext()
			{
				return _enumerator.MoveNext();
			}

			public void Reset()
			{
				_enumerator.Reset();
			}

			object IEnumerator.Current
			{
				get { return Current; }
			}

			public T Current
			{
				get { return (T) _enumerator.Current; }
			}
		}

		public EnumerableAdapter(IEnumerable enumerable)
		{
			_enumerable = enumerable;
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		public IEnumerator<T> GetEnumerator()
		{
			return new EnumeratorAdapter(_enumerable.GetEnumerator());
		}
	}
}