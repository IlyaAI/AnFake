using System;
using System.Linq;

namespace AnFake.Core
{
	public static class MsTools
	{
		public sealed class Toolset
		{
			private readonly FileSet _msBuildLocations;
			private readonly FileSet _msTestLocations;

			public Toolset(string name, Version version, FileSet msBuildLocations, FileSet msTestLocations)
			{
				if (String.IsNullOrEmpty(name))
					throw new ArgumentNullException("name", "MsTools.Toolset(name, version, msBuildLocations, msTestLocations): name must not be null or empty");
				if (version == null)
					throw new ArgumentNullException("name", "MsTools.Toolset(name, version, msBuildLocations, msTestLocations): version must not be null");
				if (msBuildLocations == null)
					throw new ArgumentNullException("name", "MsTools.Toolset(name, version, msBuildLocations, msTestLocations): msBuildLocations must not be null");
				if (msTestLocations == null)
					throw new ArgumentNullException("name", "MsTools.Toolset(name, version, msBuildLocations, msTestLocations): msTestLocations must not be null");

				Name = name;
				Version = version;

				_msBuildLocations = msBuildLocations;
				_msTestLocations = msTestLocations;
			}

			public string Name { get; private set; }

			public Version Version { get; private set; }

			public FileItem MsBuildExe { get; private set; }

			public FileItem MsTestExe { get; private set; }

			internal bool TryDetect()
			{
				var msBuild = _msBuildLocations.FirstOrDefault();
				if (msBuild == null)
					return false;

				var msTest = _msTestLocations.FirstOrDefault();
				if (msTest == null)
					return false;

				MsBuildExe = msBuild;
				MsTestExe = msTest;

				return true;
			}
		}

		public static readonly Toolset Vs2013 = new Toolset(
			"VS 2013", new Version(12, 0),
			"[ProgramFilesx86]/MSBuild/12.0/Bin/MsBuild.exe".AsFileSet(),
			"[ProgramFilesx86]/Microsoft Visual Studio 12.0/Common7/IDE/MsTest.exe".AsFileSet());

		private static readonly Toolset[] KnownToolsets = {Vs2013};

		public static Toolset Current { get; private set; }

		public static Toolset AutoDetect(params Toolset[] toolsets)
		{
			Current = toolsets.Union(KnownToolsets).FirstOrDefault(x => x.TryDetect());
			if (Current == null)
				throw new NotSupportedException(
					String.Format("Supported versions of Microsoft Visual Studio tools aren't found on your machine. Supported versions are: {0}",
						String.Join(", ", KnownToolsets.Select(x => x.Name))));

			return Current;
		}

		public static Toolset Detect(Version version)
		{
			var trimedVersion = new Version(version.Major, version.Minor);

			var ts = KnownToolsets.FirstOrDefault(x => x.Version == trimedVersion);
			if (ts == null)
				throw new NotSupportedException(
					String.Format("Version {0} of Microsoft Visual Studio tools aren't supported. Supported versions are: {1}",
						trimedVersion,
						String.Join(", ", KnownToolsets.Select(x => x.Version))));

			if (!ts.TryDetect())
				throw new NotSupportedException(
					String.Format("Version {0} of Microsoft Visual Studio tools aren't found on your machine.", trimedVersion));

			Current = ts;

			return Current;
		}
	}
}