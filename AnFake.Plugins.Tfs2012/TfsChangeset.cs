using System;
using System.Globalization;
using AnFake.Core.Integration;
using Microsoft.TeamFoundation.VersionControl.Client;

namespace AnFake.Plugins.Tfs2012
{
	internal sealed class TfsChangeset : IChangeset
	{
		private readonly Changeset _tfsChangeset;

		public TfsChangeset(Changeset tfsChangeset)
		{
			_tfsChangeset = tfsChangeset;
		}

		public string Id
		{
			get { return _tfsChangeset.ChangesetId.ToString(CultureInfo.InvariantCulture); }
		}

		public string Author
		{
			get { return _tfsChangeset.CommitterDisplayName; }
		}

		public DateTime Committed
		{
			get { return _tfsChangeset.CreationDate; }
		}

		public string Comment
		{
			get { return _tfsChangeset.Comment; }
		}
	}
}