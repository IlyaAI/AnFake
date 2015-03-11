using System;
using System.Collections.Generic;
using System.Linq;

namespace AnFake.Core
{
	public static class NuGetExtension
	{
		/// <summary>
		///		Adds references to NuSpec.
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
		///		Adds files to package.
		/// </summary>
		/// <param name="package"></param>
		/// <param name="files"></param>
		/// <param name="target"></param>
		public static void AddFiles(this NuSpec.v20.Package package, IEnumerable<FileItem> files, string target)
		{
			var nuFiles = files
				.Select(x => new NuSpec.v20.File(x.Path.Full, (target.AsPath() / x.RelPath).Spec))
				.ToArray();

			package.Files = Merge(package.Files, nuFiles);
		}

		/// <summary>
		///		Adds files to package.
		/// </summary>
		/// <param name="package"></param>
		/// <param name="files"></param>
		/// <param name="targetFramework"></param>
		public static void AddFiles(this NuSpec.v20.Package package, IEnumerable<FileItem> files, NuSpec.v20.Framework targetFramework)
		{
			AddFiles(package, files, "lib/" + targetFramework.ToString().ToLowerInvariant());
		}

		/// <summary>
		///		Adds references to NuSpec.
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
					.Select(x => new NuSpec.v25.Reference { File = x })
					.ToArray()
			};

			meta.ReferenceGroups = Merge(meta.ReferenceGroups, group);
		}

		/// <summary>
		///		Adds files to package.
		/// </summary>
		/// <param name="package"></param>
		/// <param name="files"></param>
		/// <param name="target"></param>
		public static void AddFiles(this NuSpec.v25.Package package, IEnumerable<FileItem> files, string target)
		{
			var nuFiles = files
				.Select(x => new NuSpec.v25.File(x.Path.Full, (target.AsPath() / x.RelPath).Spec))
				.ToArray();

			package.Files = Merge(package.Files, nuFiles);
		}

		/// <summary>
		///		Adds files to package.
		/// </summary>
		/// <param name="package"></param>
		/// <param name="files"></param>
		/// <param name="targetFramework"></param>
		public static void AddFiles(this NuSpec.v25.Package package, IEnumerable<FileItem> files, NuSpec.v25.Framework targetFramework)
		{
			AddFiles(package, files, "lib/" + targetFramework.ToString().ToLowerInvariant());
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
				return new[] { item };

			var merged = srcArray;
			var count = srcArray.Length;
			Array.Resize(ref merged, count + 1);
			merged[count] = item;

			return merged;
		}
	}
}