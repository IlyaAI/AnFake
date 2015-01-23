using System;

namespace AnFake.Core.Integration.Tracking
{
	public sealed class ReleaseNote
	{
		public ReleaseNote(string id)
		{
			Id = id;
		}

		public string Id { get; private set; }

		public Uri Uri { get; set; }

		public string Category { get; set; }

		public string Summary { get; set; }

		public string State { get; set; }
	}
}