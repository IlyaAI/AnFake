using System;
using Microsoft.TeamFoundation.VersionControl.Client;

namespace AnFake.Integration.Tfs2012
{
	public sealed class ExtendedMapping
	{
		public readonly WorkingFolder WorkingFolder;
		public readonly VersionSpec VersionSpec;

		private ExtendedMapping(WorkingFolder workingFolder, VersionSpec versionSpec)
		{
			WorkingFolder = workingFolder;
			VersionSpec = versionSpec;
		}

		public string ServerItem
		{
			get { return WorkingFolder.ServerItem; }
		}

		public string LocalItem
		{
			get { return WorkingFolder.LocalItem; }
		}

		public bool IsCloaked
		{
			get { return WorkingFolder.IsCloaked; }
		}

		public RecursionType Depth
		{
			get { return WorkingFolder.Depth; }
		}

		public static ExtendedMapping Map(string serverItem, string localItem)
		{
			return new ExtendedMapping(new WorkingFolder(serverItem, localItem), null);
		}

		public static ExtendedMapping Map(string serverItem, string localItem, VersionSpec versionSpec)
		{
			return new ExtendedMapping(new WorkingFolder(serverItem, localItem), versionSpec);
		}

		public static ExtendedMapping Cloak(string serverItem)
		{
			return new ExtendedMapping(new WorkingFolder(serverItem, String.Empty, WorkingFolderType.Cloak), null);
		}
	}
}