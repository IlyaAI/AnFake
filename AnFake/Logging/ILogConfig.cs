using AnFake.Api;

namespace AnFake.Logging
{
	public interface ILogConfig
	{
		void SetUp(string logPath, Verbosity verbosity);
	}
}