namespace AnFake.Core.Exceptions
{
	public sealed class InvalidConfigurationException : AnFakeException
	{
		public InvalidConfigurationException(string message) : base(message)
		{
		}		
	}
}