using System;

namespace AnFake.Core
{
	public static class Targets
	{
		public static Target AsTarget(this string name)
		{
			if (String.IsNullOrEmpty(name))
				throw new ArgumentNullException("name", "Targets.AsTarget(name, action): name must not be null or empty");

			return Target.GetOrCreate(name);
		}		
	}
}