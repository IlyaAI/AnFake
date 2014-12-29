using System;

namespace AnFake.Core.NuSpec
{
	public interface IPackage
	{
		string Id { get; }

		Version Version { get; }

		void Validate();
	}
}