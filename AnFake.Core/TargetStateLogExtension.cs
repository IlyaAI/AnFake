using AnFake.Api;

namespace AnFake.Core
{
	internal static class LogEx
	{
		public static void TargetState(TargetState state, string message)
		{
			switch (state)
			{
				case Core.TargetState.Succeeded:
					Log.Success(message);
					break;
				case Core.TargetState.PartiallySucceeded:
					Log.Warn(message);
					break;
				case Core.TargetState.Failed:
					Log.Error(message);
					break;
			}
		}

		public static void TargetStateFormat(TargetState state, string format, params object[] args)
		{
			switch (state)
			{
				case Core.TargetState.Succeeded:
					Log.SuccessFormat(format, args);
					break;
				case Core.TargetState.PartiallySucceeded:
					Log.WarnFormat(format, args);
					break;
				case Core.TargetState.Failed:
					Log.ErrorFormat(format, args);
					break;
			}
		}
	}
}