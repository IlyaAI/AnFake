using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.TeamFoundation.VersionControl.Client;

namespace AnFake.Integration.Tfs2012
{
	public static class VcsMappings
	{
		public const string CommentMarker = "#";		
		public const string CloackedMarker = "-";
		public const char Separator = ':';

		public static ExtendedMapping[] Parse(string workspace, string serverPath, string localPath)
		{
			if (workspace == null)
				throw new ArgumentException(String.Format("VcsMappings.Parse(workspace, localPath): workspace must not be null"));
			if (localPath == null)
				throw new ArgumentException(String.Format("VcsMappings.Parse(workspace, localPath): localPath must not be null"));
			if (!Path.IsPathRooted(localPath))
				throw new ArgumentException(String.Format("VcsMappings.Parse(workspace, localPath): localPath must be an absolute path"));

			var folders = new List<ExtendedMapping>
			{
				ExtendedMapping.Map(serverPath, localPath)
			};

			foreach (var mappingLine in workspace.Split('\n', '\r').Select(x => x.Trim()).Where(x => x.Length > 0))
			{
				if (mappingLine.StartsWith(CommentMarker))
					continue;
				
				var cloacked = mappingLine.StartsWith(CloackedMarker);

				if (cloacked)
				{
					var mapFrom = ServerPathUtils.Combine(
						serverPath,
						mappingLine.Remove(0, CloackedMarker.Length).Trim(' ', Separator));					

					folders.Add(ExtendedMapping.Cloak(mapFrom));
				}
				else
				{
					var mappingParts = mappingLine.Split(Separator);
					if (mappingParts.Length != 2)
						throw new FormatException(
							String.Format(
								"Invalid mapping detected: '{0}'.\nMapping must contain one and only one separator character. E.g. '$/tfs/path{1} subpath'.",
								mappingLine,
								Separator));

					var mapFrom = ServerPathUtils.Combine(
						serverPath,
						mappingParts[0].Trim());

					VersionSpec[] versionSpecs;
					VersionSpec.ParseVersionedFileSpec(mapFrom, ".", out mapFrom, out versionSpecs);
					
					var mapTo = mappingParts[1].Trim();
					if (Path.IsPathRooted(mapTo))
						throw new FormatException(
							String.Format(@"Invalid mapping detected: '{0}'.\nMapping target path must be relative.", mappingLine));

					mapTo = Path.Combine(localPath, mapTo);

					folders.Add(ExtendedMapping.Map(mapFrom, mapTo, versionSpecs.FirstOrDefault()));
				}
			}

			return folders.ToArray();
		}

		public static WorkingFolder[] AsTfsMappings(this IEnumerable<ExtendedMapping> mappings)
		{
			return mappings.Select(x => x.WorkingFolder).ToArray();
		}
	}
}