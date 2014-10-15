using System;
using System.Collections;
using System.Collections.Generic;

namespace AnFake.Core
{
	public sealed class FolderSet : IEnumerable<FolderItem>
	{
		private enum PatternType
		{
			Include,
			Exclude
		};

		private class Pattern
		{
			public readonly PatternType Type;
			public readonly FileSystemPath WildcardedPath;

			public Pattern(PatternType type, FileSystemPath wildcardedPath)
			{
				Type = type;
				WildcardedPath = wildcardedPath;
			}
		}

		private readonly List<Pattern> _patterns = new List<Pattern>();

		public FolderSet Include(FileSystemPath wildcardedPath)
		{
			_patterns.Add(new Pattern(PatternType.Include, wildcardedPath));

			return this;
		}

		public FolderSet Include(string wildcardedPath)
		{
			return Include(wildcardedPath.AsPath());
		}

		public FolderSet Exclude(FileSystemPath wildcardedPath)
		{
			_patterns.Add(new Pattern(PatternType.Exclude, wildcardedPath));

			return this;
		}

		public FolderSet Exclude(string wildcardedPath)
		{
			return Exclude(wildcardedPath.AsPath());
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		public IEnumerator<FolderItem> GetEnumerator()
		{
			var folders = new SortedSet<FolderItem>();

			foreach (var pattern in _patterns)
			{
				MergeFolders(
					pattern.Type,
					FileSystem.MatchFolders(pattern.WildcardedPath),
					folders);
			}

			return folders.GetEnumerator();
		}

		public static FolderSet operator +(FolderSet folders, FileSystemPath wildcardedPath)
		{
			return folders.Include(wildcardedPath);
		}

		public static FolderSet operator +(FolderSet folders, string wildcardedPath)
		{
			return folders.Include(wildcardedPath);
		}

		public static FolderSet operator -(FolderSet folders, FileSystemPath wildcardedPath)
		{
			return folders.Exclude(wildcardedPath);
		}

		public static FolderSet operator -(FolderSet folders, string wildcardedPath)
		{
			return folders.Exclude(wildcardedPath);
		}

		private static void MergeFolders(PatternType mergeType, IEnumerable<FileSystemPath> src, ISet<FolderItem> dst)
		{
			if (mergeType == PatternType.Include)
			{
				foreach (var fn in src)
				{
					dst.Add(new FolderItem(fn));
				}

				return;
			}

			if (mergeType == PatternType.Exclude)
			{
				foreach (var fn in src)
				{
					dst.Remove(new FolderItem(fn));
				}

				return;
			}

			throw new NotSupportedException("Include/Exclude supported only.");
		}
	}
}