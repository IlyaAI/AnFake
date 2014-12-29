using System;

namespace AnFake.Core.Integration
{
	public interface IChangeset
	{
		int Id { get; }

		string Author { get; }

		DateTime Committed { get; }

		string Comment { get; }
	}
}