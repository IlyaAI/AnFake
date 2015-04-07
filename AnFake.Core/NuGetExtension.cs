using System;
using System.Collections.Generic;
using System.Linq;

namespace AnFake.Core
{
	public static class NuGetExtension
	{
		/// <summary>
		///     Adds references to NuSpec.
		/// </summary>
		/// <param name="meta"></param>
		/// <param name="assemblies"></param>
		public static void AddRefs(this NuSpec.v20.Metadata meta, IEnumerable<string> assemblies)
		{
			var refs = assemblies
				.Select(x => new NuSpec.v20.Reference {File = x})
				.ToArray();

			meta.References = Merge(meta.References, refs);
		}

		/// <summary>
		///     Adds files to package.
		/// </summary>
		/// <param name="package"></param>
		/// <param name="files"></param>
		/// <param name="target"></param>
		public static void AddFiles(this NuSpec.v20.Package package, IEnumerable<FileItem> files, string target = "")
		{
			var nuFiles = files
				.Select(x => new NuSpec.v20.File(x.Path.Full, (target.AsPath()/x.RelPath).Spec))
				.ToArray();

			package.Files = Merge(package.Files, nuFiles);
		}

		/// <summary>
		///     Adds files to package.
		/// </summary>
		/// <param name="package"></param>
		/// <param name="files"></param>
		/// <param name="targetFramework"></param>
		public static void AddFiles(this NuSpec.v20.Package package, IEnumerable<FileItem> files, NuSpec.v20.Framework targetFramework)
		{
			AddFiles(package, files, "lib/" + targetFramework.ToString().ToLowerInvariant());
		}

		/// <summary>
		///     Adds dependencies to NuSpec.
		/// </summary>
		/// <param name="meta">package metadata (not null)</param>
		/// <param name="packageId">id of the package that this package is dependent upon (not null)</param>
		/// <param name="version">range of versions acceptable as a dependency (not null); String and Version types accepted</param>
		/// <param name="packageIdVersionPairs">packageId-version pairs for additional dependencies</param>
		/// <seealso cref="http://docs.nuget.org/create/nuspec-reference#specifying-dependencies" />
		public static void AddDependencies(this NuSpec.v20.Metadata meta, string packageId, object version, params object[] packageIdVersionPairs)
		{
			if (meta == null)
				throw new ArgumentException("NuGetExtension.AddDependencies(meta, ...): meta must not be null");

			meta.Dependencies = Merge(
				meta.Dependencies, 
				ToDependenciesArray20(packageId, version, packageIdVersionPairs));			
		}

		/// <summary>
		///     Adds references to NuSpec.
		/// </summary>
		/// <param name="meta"></param>
		/// <param name="targetFramework"></param>
		/// <param name="assemblies"></param>
		public static void AddRefs(this NuSpec.v25.Metadata meta, NuSpec.v25.Framework targetFramework, IEnumerable<string> assemblies)
		{
			var group = new NuSpec.v25.ReferenceGroup
			{
				TargetFramework = targetFramework,
				References = assemblies
					.Select(x => new NuSpec.v25.Reference {File = x})
					.ToArray()
			};

			meta.ReferenceGroups = Merge(meta.ReferenceGroups, group);
		}

		/// <summary>
		///     Adds files to package.
		/// </summary>
		/// <param name="package"></param>
		/// <param name="files"></param>
		/// <param name="target"></param>
		public static void AddFiles(this NuSpec.v25.Package package, IEnumerable<FileItem> files, string target = "")
		{
			var nuFiles = files
				.Select(x => new NuSpec.v25.File(x.Path.Full, (target.AsPath()/x.RelPath).Spec))
				.ToArray();

			package.Files = Merge(package.Files, nuFiles);
		}

		/// <summary>
		///     Adds files to package.
		/// </summary>
		/// <param name="package"></param>
		/// <param name="files"></param>
		/// <param name="targetFramework"></param>
		public static void AddFiles(this NuSpec.v25.Package package, IEnumerable<FileItem> files, NuSpec.v25.Framework targetFramework)
		{
			AddFiles(package, files, "lib/" + targetFramework.ToString().ToLowerInvariant());
		}

		/// <summary>
		///     Adds framework specific dependencies to NuSpec.
		/// </summary>
		/// <param name="meta">package metadata (not null)</param>
		/// <param name="targetFramework">target .net framework</param>
		/// <param name="packageId">id of the package that this package is dependent upon (not null or empty)</param>
		/// <param name="version">range of versions acceptable as a dependency (not null); String and Version types accepted</param>
		/// <param name="packageIdVersionPairs">packageId-version pairs for additional dependencies</param>
		/// <seealso cref="http://docs.nuget.org/create/nuspec-reference#specifying-dependencies" />
		public static void AddDependencies(
			this NuSpec.v25.Metadata meta, NuSpec.v25.Framework targetFramework,
			string packageId, object version, params object[] packageIdVersionPairs)
		{
			if (meta == null)
				throw new ArgumentException("NuGetExtension.AddDependencies(meta, ...): meta must not be null");

			var group = new NuSpec.v25.DependencyGroup
			{
				TargetFramework = targetFramework,
				Dependencies = ToDependenciesArray25(packageId, version, packageIdVersionPairs)
			};

			meta.DependencyGroups = Merge(meta.DependencyGroups, group);
		}

		/// <summary>
		///     Adds dependencies to NuSpec.
		/// </summary>
		/// <param name="meta">package metadata (not null)</param>
		/// <param name="packageId">id of the package that this package is dependent upon (not null)</param>
		/// <param name="version">range of versions acceptable as a dependency (not null); String and Version types accepted</param>
		/// <param name="packageIdVersionPairs">packageId-version pairs for additional dependencies</param>
		/// <seealso cref="http://docs.nuget.org/create/nuspec-reference#specifying-dependencies" />
		public static void AddDependencies(this NuSpec.v25.Metadata meta, string packageId, object version, params object[] packageIdVersionPairs)
		{
			if (meta == null)
				throw new ArgumentException("NuGetExtension.AddDependencies(meta, ...): meta must not be null");

			var group = new NuSpec.v25.DependencyGroup
			{
				Dependencies = ToDependenciesArray25(packageId, version, packageIdVersionPairs)
			};

			meta.DependencyGroups = Merge(meta.DependencyGroups, group);
		}

		private static NuSpec.v20.Dependency[] ToDependenciesArray20(string packageId, object version, object[] packageIdVersionPairs)
		{
			if (String.IsNullOrEmpty(packageId))
				throw new ArgumentException("NuGetExtension.AddDependencies(..., packageId, version[, ...]): packageId must not be null or empty");
			if (version == null || version.ToString() == String.Empty)
				throw new ArgumentException("NuGetExtension.AddDependencies(..., packageId, version[, ...]): version must not be null or empty");
			if (packageIdVersionPairs.Length % 2 != 0)
				throw new ArgumentException("NuGetExtension.AddDependencies(..., packageId, version[, ...]): both packageId and version must be specified");

			var deps = new List<NuSpec.v20.Dependency>
			{
				new NuSpec.v20.Dependency
				{
					Id = packageId,
					Version = version.ToString()
				}
			};

			for (var i = 0; i + 1 < packageIdVersionPairs.Length; i += 2)
			{
				deps.Add(
					new NuSpec.v20.Dependency
					{
						Id = packageIdVersionPairs[i].ToString(),
						Version = packageIdVersionPairs[i + 1].ToString()
					});
			}

			return deps.ToArray();
		}

		private static NuSpec.v25.Dependency[] ToDependenciesArray25(string packageId, object version, object[] packageIdVersionPairs)
		{
			if (String.IsNullOrEmpty(packageId))
				throw new ArgumentException("NuGetExtension.AddDependencies(..., packageId, version[, ...]): packageId must not be null or empty");
			if (version == null || version.ToString() == String.Empty)
				throw new ArgumentException("NuGetExtension.AddDependencies(..., packageId, version[, ...]): version must not be null or empty");
			if (packageIdVersionPairs.Length % 2 != 0)
				throw new ArgumentException("NuGetExtension.AddDependencies(..., packageId, version[, ...]): both packageId and version must be specified");

			var deps = new List<NuSpec.v25.Dependency>
			{
				new NuSpec.v25.Dependency
				{
					Id = packageId,
					Version = version.ToString()
				}
			};

			for (var i = 0; i + 1 < packageIdVersionPairs.Length; i += 2)
			{
				deps.Add(
					new NuSpec.v25.Dependency
					{
						Id = packageIdVersionPairs[i].ToString(),
						Version = packageIdVersionPairs[i + 1].ToString()
					});
			}

			return deps.ToArray();
		}

		private static T[] Merge<T>(T[] srcArray, T[] addArray)
		{
			if (srcArray == null)
				return addArray;

			var merged = srcArray;
			var count = srcArray.Length;
			Array.Resize(ref merged, count + addArray.Length);
			Array.Copy(addArray, 0, merged, count, addArray.Length);

			return merged;
		}

		private static T[] Merge<T>(T[] srcArray, T item)
		{
			if (srcArray == null)
				return new[] {item};

			var merged = srcArray;
			var count = srcArray.Length;
			Array.Resize(ref merged, count + 1);
			merged[count] = item;

			return merged;
		}
	}
}