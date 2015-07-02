using System;
using System.IO;
using AnFake.Core.Exceptions;

namespace AnFake.Core
{
	internal static class Mono
	{
		private static string _lib;
		private static string _bin;

		static Mono()
		{
			var monoRuntime = Type.GetType("Mono.Runtime");
			if (monoRuntime == null)
				return;

			var di = new FileInfo(monoRuntime.Assembly.Location).Directory;
			if (di == null)
				throw new InvalidOperationException("Unable to detect Mono lib path.");

			Lib = di.FullName; // .../lib/mono/x.y

			var parent = di.Parent;
			for (var i = 0; i < 2 && parent != null; i++)
			{
				parent = parent.Parent;
			}

			if (parent != null)
			{
				var bin = Path.Combine(parent.FullName, "bin"); // .../bin
				if (Directory.Exists(bin))
				{
					Bin = bin;
				}
			}

			MyBuild.Initialized += (s, p) =>
			{
				string value;
				if (p.Properties.TryGetValue("Mono.Bin", out value))
				{
					Bin = value;
				}				
			};
		}

		public static string Lib
		{
			get
			{
				if (_lib == null)
					throw new InvalidConfigurationException("Mono.Lib is supported only under Mono run.");

				return _lib;
			}
			private set
			{
				_lib = value;
			}
		}

		public static string Bin
		{
			get
			{
				if (_bin == null)
				{
					if (_lib != null)
						throw new InvalidConfigurationException("Mono.Bin wasn't detected automatically. Try to set it manually via command line as 'Mono.Bin=/path/to/mono/bin'.");
					
					throw new InvalidConfigurationException("Mono.Bin is supported only under Mono run.");
				}
					
				return _bin;
			}
			private set
			{
				_bin = value;
			}
		}
	}
}
