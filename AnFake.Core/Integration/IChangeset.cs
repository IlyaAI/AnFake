using System;

namespace AnFake.Core.Integration
{
	public interface IChangeset
	{
		string Id { get; }

		string Author { get; }

		DateTime Committed { get; }

		string Comment { get; }
	}
}