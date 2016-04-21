using System.Collections.Generic;
using AnFake.Api;

namespace AnFake
{
	internal sealed class RunOptions
	{
		public readonly IDictionary<string, string> Properties = new Dictionary<string, string>();
		public readonly IList<string> Targets = new List<string>();
		public Verbosity Verbosity = Verbosity.Normal;
		public string Script = "build.fsx";
		public string BuildPath;
		public string LogPath;
		public bool IsDebug;
	}
}