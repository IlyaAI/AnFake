using System;

namespace AnFake.Core
{
	public static class TargetExtension
	{
		public static Target AsTarget(this string name)
		{
			if (String.IsNullOrEmpty(name))
				throw new ArgumentException("TargetExtension.AsTarget(name): name must not be null or empty");

			return Target.GetOrCreate(name);
		}

		public static string ToHumanReadable(this TargetState state)
		{
			switch (state)
			{				
				case TargetState.PartiallySucceeded:
					return "Partially Succeeded";

				default:
					return state.ToString();
			}
		}
	}
}