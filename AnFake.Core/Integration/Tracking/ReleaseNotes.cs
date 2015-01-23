using System;
using System.Collections.Generic;
using System.Linq;

namespace AnFake.Core.Integration.Tracking
{
	public sealed class ReleaseNotes
	{
		public string ProductName { get; internal set; }

		public Version ProductVersion { get; internal set; }

		public DateTime ReleaseDate { get; internal set; }

		public IEnumerable<IGrouping<string, ReleaseNote>> CategorizedNotes { get; internal set; }
	}
}