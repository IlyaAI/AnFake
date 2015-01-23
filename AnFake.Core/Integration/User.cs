using System.Security.Principal;
using AnFake.Core.Exceptions;

namespace AnFake.Core.Integration
{
	public static class User
	{
		public static string Current
		{
			get
			{
				var identity = WindowsIdentity.GetCurrent();
				if (identity == null)
					throw new InvalidConfigurationException("Current user isn't authenticated.");

				return identity.Name;
			}
		}
	}
}