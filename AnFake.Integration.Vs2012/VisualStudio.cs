using System;
using System.Collections.Generic;
using Microsoft.Win32;

namespace AnFake.Integration.Vs2012
{
	public static class VisualStudio
	{
		private const string KeyBase = @"Software\Microsoft\VisualStudio";
		private const string KeyExternalTools = @"External Tools";

		public static IEnumerable<Version> GetInstalledVersions()
		{
			using (var key = Registry.CurrentUser.OpenSubKey(KeyBase))
			{
				if (key == null)
					yield break;

				foreach (var subKey in key.GetSubKeyNames())
				{
					Version version;
					if (Version.TryParse(subKey, out version))
					{
						yield return version;
					}
				}
			}
		}

		public static List<ExternalTool> GetExternalTools(Version version)
		{
			using (var key = OpenSubKey(Registry.CurrentUser, version, KeyExternalTools))
			{
				return ExternalTool.Read(key);
			}
		}

		public static void SetExternalTools(Version version, List<ExternalTool> tools)
		{
			using (var key = OpenSubKey(Registry.CurrentUser, version, KeyExternalTools))
			{
				ExternalTool.Write(key, tools);
			}
		}

		private static RegistryKey OpenSubKey(RegistryKey parent, Version version, string subKey)
		{
			var keyPath = String.Format(@"{0}\{1}\{2}", KeyBase, version, subKey);
			var key = parent.OpenSubKey(keyPath);
			if (key == null)
				throw new InvalidOperationException(String.Format(@"Unable to open registry key '{0}\{1}'.", parent.Name, keyPath));

			return key;
		}
	}
}