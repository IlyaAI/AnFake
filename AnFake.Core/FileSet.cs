using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AnFake.Core
{
	/// <summary>
	///     Represents a set of files matched by specified wildcard patterns.
	/// </summary>
	/// <remarks>
	///     FileSet is instantiated from string or FileSystemPath by an extension method AsFileSet or by operator !! in F#, or
	///     from FileSystemPath by operator % followed by wildcard (means files matching pattern in given folder).
	/// </remarks>
	public sealed class FileSet : IEnumerable<FileItem>
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
			public readonly FileSystemPath BasePath;

			public Pattern(PatternType type, FileSystemPath wildcardedPath, FileSystemPath basePath)
			{
				Type = type;
				WildcardedPath = wildcardedPath;
				BasePath = basePath;
			}
		}

		private readonly List<Pattern> _patterns = new List<Pattern>();
		private FileSystemPath _basePath;

		internal FileSet()
		{
			_basePath = "".AsPath();
		}

		public FileSet Include(FileSystemPath wildcardedPath)
		{
			if (wildcardedPath == null)
				throw new ArgumentException("FileSet.Include(wildcardedPath): wildcardedPath must not be null");

			_patterns.Add(new Pattern(PatternType.Include, wildcardedPath, _basePath));

			return this;
		}

		public FileSet Include(string wildcardedPath)
		{
			if (wildcardedPath == null)
				throw new ArgumentException("FileSet.Include(wildcardedPath): wildcardedPath must not be null");

			return Include(wildcardedPath.AsPath());
		}

		public FileSet Include(FileSet otherFiles)
		{
			if (otherFiles == null)
				throw new ArgumentException("FileSet.Include(otherFiles): otherFiles must not be null");

			_patterns.AddRange(otherFiles._patterns);

			return this;
		}

		public FileSet Exclude(FileSystemPath wildcardedPath)
		{
			if (wildcardedPath == null)
				throw new ArgumentException("FileSet.Exclude(wildcardedPath): wildcardedPath must not be null");

			_patterns.Add(new Pattern(PatternType.Exclude, wildcardedPath, _basePath));

			return this;
		}

		public FileSet Exclude(string wildcardedPath)
		{
			if (wildcardedPath == null)
				throw new ArgumentException("FileSet.Exclude(wildcardedPath): wildcardedPath must not be null");

			return Exclude(wildcardedPath.AsPath());
		}

		public FileSet Exclude(FileSet otherFiles)
		{
			if (otherFiles == null)
				throw new ArgumentException("FileSet.Exclude(otherFiles): otherFiles must not be null");

			_patterns.AddRange(
				otherFiles._patterns.Select(
					x => x.Type == PatternType.Include
						? new Pattern(PatternType.Exclude, x.WildcardedPath, x.BasePath)
						: new Pattern(PatternType.Include, x.WildcardedPath, x.BasePath)));

			return this;
		}

		public FileSet From(FileSystemPath basePath)
		{
			if (basePath == null)
				throw new ArgumentException("FileSet.From(basePath): basePath must not be null");

			_basePath = basePath;

			return this;
		}

		public FileSet From(string basePath)
		{
			if (basePath == null)
				throw new ArgumentException("FileSet.From(basePath): basePath must not be null");

			return From(basePath.AsPath());
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		public IEnumerator<FileItem> GetEnumerator()
		{
			/*var files = new List<FileItem>();

			foreach (var pattern in _patterns)
			{
				if (pattern.BasePath.IsWildcarded)
				{
					foreach (var folder in FileSystem.MatchFolders(pattern.BasePath))
					{
						MergeFiles(
							pattern.Type,
							folder,
							FileSystem.MatchFiles(folder, pattern.WildcardedPath),
							files);
					}
				}
				else
				{
					MergeFiles(
						pattern.Type,
						pattern.BasePath,
						FileSystem.MatchFiles(pattern.BasePath, pattern.WildcardedPath),
						files);
				}
			}

			return files.Distinct().GetEnumerator();*/

			return SearchIn(FileSystemPath.Base).GetEnumerator();
		}

		public IEnumerable<FileItem> SearchIn(FileSystemPath searchPath)
		{
			var files = new List<FileItem>();

			foreach (var pattern in _patterns)
			{
				var basePath = searchPath / pattern.BasePath;
				if (basePath.IsWildcarded)
				{
					foreach (var folder in FileSystem.MatchFolders(basePath))
					{
						MergeFiles(
							pattern.Type,
							folder,
							FileSystem.MatchFiles(folder, pattern.WildcardedPath),
							files);
					}
				}
				else
				{
					MergeFiles(
						pattern.Type,
						basePath,
						FileSystem.MatchFiles(basePath, pattern.WildcardedPath),
						files);
				}
			}

			return files.Distinct();
		}

		public override string ToString()
		{
			if (_patterns.Count == 0)
				return "(no files)";

			var sb = new StringBuilder(128);
			foreach (var pattern in _patterns)
			{
				if (sb.Length > 64)
				{
					sb.Append("...");
					break;
				}

				switch (pattern.Type)
				{
					case PatternType.Include:
						if (sb.Length > 0)
						{
							sb.Append(" + ");
						}						
						break;
					case PatternType.Exclude:
						if (sb.Length > 0)
						{
							sb.Append(" - ");
						}
						else
						{
							sb.Append('-');
						}
						break;
				}

				if (pattern.BasePath.Spec.Length == 0)
				{
					sb.Append("!!'").Append(pattern.WildcardedPath).Append('\'');
					
				}
				else
				{
					sb.Append('\'').Append(pattern.BasePath).Append("' % '")
						.Append(pattern.WildcardedPath).Append('\'');
				}
			}

			return sb.ToString();
		}

		public static FileSet Empty()
		{
			return new FileSet();
		}

		public static FileSet operator +(FileSet files, FileSystemPath wildcardedPath)
		{
			return files.Include(wildcardedPath);
		}

		public static FileSet operator +(FileSet files, string wildcardedPath)
		{
			return files.Include(wildcardedPath);
		}

		public static FileSet operator +(FileSet files, FileSet otherFiles)
		{
			return files.Include(otherFiles);
		}

		public static FileSet operator -(FileSet files, FileSystemPath wildcardedPath)
		{
			return files.Exclude(wildcardedPath);
		}

		public static FileSet operator -(FileSet files, string wildcardedPath)
		{
			return files.Exclude(wildcardedPath);
		}

		public static FileSet operator -(FileSet files, FileSet otherFiles)
		{
			return files.Exclude(otherFiles);
		}		

		private static void MergeFiles(PatternType mergeType, FileSystemPath basePath, IEnumerable<FileSystemPath> src, ICollection<FileItem> dst)
		{
			if (mergeType == PatternType.Include)
			{
				foreach (var fn in src)
				{
					dst.Add(new FileItem(fn, basePath));
				}

				return;
			}

			if (mergeType == PatternType.Exclude)
			{
				foreach (var fn in src)
				{
					dst.Remove(new FileItem(fn, basePath));
				}

				return;
			}

			throw new NotSupportedException("Include/Exclude supported only.");
		}
	}
}