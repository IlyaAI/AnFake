using Common.Logging;

namespace AnFake.Core
{
	internal static class TargetStateLogExtension
	{
		public static void TargetState(this ILog log, TargetState state, string message)
		{
			switch (state)
			{
				case Core.TargetState.Succeeded:
					log.Info(message);
					break;
				case Core.TargetState.PartiallySucceeded:
					log.Warn(message);
					break;
				case Core.TargetState.Failed:
					log.Error(message);
					break;
			}
		}

		public static void TargetStateFormat(this ILog log, TargetState state, string format, params object[] args)
		{
			switch (state)
			{
				case Core.TargetState.Succeeded:
					log.InfoFormat(format, args);
					break;
				case Core.TargetState.PartiallySucceeded:
					log.WarnFormat(format, args);
					break;
				case Core.TargetState.Failed:
					log.ErrorFormat(format, args);
					break;
			}
		}
	}
}