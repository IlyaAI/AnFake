using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using AnFake.Core.Exceptions;
using Common.Logging;

namespace AnFake.Core
{
	public static class FileSystem
	{
		private static readonly ILog Log = LogManager.GetLogger(typeof (FileSystem).FullName);

		private static readonly string DirectorySeparatorString = Path.DirectorySeparatorChar.ToString(CultureInfo.InvariantCulture);

		public sealed class Params
		{
			public int MaxRetries;
			public TimeSpan RetryInterval;
			public FileAttributes JunkFileAttributes;

			public Params()
			{
				MaxRetries = 4;
				RetryInterval = TimeSpan.FromMilliseconds(50);
				JunkFileAttributes = FileAttributes.Hidden | FileAttributes.ReadOnly;
			}
		}

		static FileSystem()
		{		
			Defaults = new Params();
		}		

		public static Params Defaults { get; private set; }

		public static FileSystemPath AsPath(this string path)
		{
			if (path == null)
				throw new AnFakeArgumentException("FileSystem.AsPath(path): path must not be null");

			return new FileSystemPath(path, false);
		}		

		public static FileSystemPath AsPath(this string[] pathSteps, int start, int count)
		{
			if (pathSteps == null)
				throw new AnFakeArgumentException("FileSystem.AsPath(pathSteps): pathSteps must not be null");

			return new FileSystemPath(String.Join(DirectorySeparatorString, pathSteps, start, count), true);
		}

		public static FileItem AsFile(this FileSystemPath path)
		{
			if (path == null)
				throw new AnFakeArgumentException("FileSystem.AsFile(path): path must not be null");

			return new FileItem(path, FileSystemPath.Base);
		}

		public static FileItem AsFile(this string path)
		{
			return AsFile(path.AsPath());
		}

		public static FileSet AsFileSet(this FileSystemPath wildcardedPath)
		{
			if (wildcardedPath == null)
				throw new AnFakeArgumentException("FileSystem.AsFileSet(wildcardedPath): wildcardedPath must not be null");

			return new FileSet().Include(wildcardedPath);
		}

		public static FileSet AsFileSet(this FileSystemPath[] wildcardedPathes)
		{
			if (wildcardedPathes == null)
				throw new AnFakeArgumentException("FileSystem.AsFileSet(wildcardedPathes): wildcardedPathes must not be null");

			var fs = new FileSet();
			foreach (var path in wildcardedPathes)
			{
				fs.Include(path);
			}

			return fs;
		}

		public static FileSet AsFileSet(this string wildcardedPath)
		{
			return AsFileSet(wildcardedPath.AsPath());
		}

		public static FileSet AsFileSet(this string[] wildcardedPathes)
		{
			if (wildcardedPathes == null)
				throw new AnFakeArgumentException("FileSystem.AsFileSet(wildcardedPathes): wildcardedPathes must not be null");

			var fs = new FileSet();
			foreach (var path in wildcardedPathes)
			{
				fs.Include(path);
			}

			return fs;
		}

		public static FileSet AsFileSetFrom(this FileSystemPath wildcardedPath, FileSystemPath basePath)
		{
			if (wildcardedPath == null)
				throw new AnFakeArgumentException("FileSystem.AsFileSetFrom(wildcardedPath, basePath): wildcardedPath must not be null");
			if (basePath == null)
				throw new AnFakeArgumentException("FileSystem.AsFileSetFrom(wildcardedPath, basePath): basePath must not be null");

			return new FileSet().From(basePath)
				.Include(wildcardedPath);
		}

		public static FileSet AsFileSetFrom(this FileSystemPath wildcardedPath, string basePath)
		{
			return AsFileSetFrom(wildcardedPath, basePath.AsPath());
		}

		public static FileSet AsFileSetFrom(this string wildcardedPath, FileSystemPath basePath)
		{
			return AsFileSetFrom(wildcardedPath.AsPath(), basePath);
		}

		public static FileSet AsFileSetFrom(this string wildcardedPath, string basePath)
		{
			return AsFileSetFrom(wildcardedPath.AsPath(), basePath.AsPath());
		}

		public static FolderItem AsFolder(this FileSystemPath path)
		{
			if (path == null)
				throw new AnFakeArgumentException("FileSystem.AsFolder(path): path must not be null");

			return new FolderItem(path);
		}

		public static FolderItem AsFolder(this string path)
		{
			return AsFolder(path.AsPath());
		}

		public static FolderSet AsFolderSet(this FileSystemPath wildcardedPath)
		{
			if (wildcardedPath == null)
				throw new AnFakeArgumentException("FileSystem.AsFolderSet(wildcardedPath): wildcardedPath must not be null");

			return new FolderSet().Include(wildcardedPath);
		}

		public static FolderSet AsFolderSet(this string wildcardedPath)
		{
			return AsFolderSet(wildcardedPath.AsPath());
		}
		
		public static string MakeUnique(this string name, string ext)
		{
			return String.Format("{0}.{1:yyyyMMdd.HHmmss.ff}{2}", name, DateTime.Now, ext);
		}

		public static string MakeUnique(this string name)
		{
			return MakeUnique(name, "");
		}

		internal static IEnumerable<FileSystemPath> MatchFiles(FileSystemPath basePath, FileSystemPath wildcardedPath)
		{
			return Match(basePath, wildcardedPath, Directory.EnumerateFiles);
		}

		internal static IEnumerable<FileSystemPath> MatchFolders(FileSystemPath wildcardedPath)
		{
			return Match(FileSystemPath.Base, wildcardedPath, Directory.EnumerateDirectories);
		}

		internal static void DeleteFiles(IEnumerable<FileSystemPath> files)
		{
			Delete(files, FileEraser);
		}

		internal static void DeleteFile(FileSystemPath file)
		{
			Delete(new[] {file}, FileEraser);
		}

		internal static void DeleteFolders(IEnumerable<FileSystemPath> folders)
		{
			Delete(folders, FolderEraser);
		}

		internal static void DeleteFolder(FileSystemPath folder)
		{
			Delete(new[] {folder}, FolderEraser);
		}

		private static IEnumerable<FileSystemPath> Match(FileSystemPath basePath, FileSystemPath wildcardedPath, Func<string, string, SearchOption, IEnumerable<string>> enumerateEntries)
		{
			var results = new List<FileSystemPath>();

			var subPatterns = new Queue<Tuple<FileSystemPath, FileSystemPath>>();
			subPatterns.Enqueue(new Tuple<FileSystemPath, FileSystemPath>(basePath, wildcardedPath));

			do
			{
				var pattern = subPatterns.Dequeue();

				var steps = pattern.Item2.Split();
				for (var i = 0; i < steps.Length; i++)
				{
					FileSystemPath tail;

					if (steps[i] == "**")
					{
						var path = (pattern.Item1 / steps.AsPath(0, i)).Full;
						if (!Directory.Exists(path))
							break;

						if (i + 2 == steps.Length)
						{
							results.AddRange(enumerateEntries(path, steps[i + 1], SearchOption.AllDirectories).Select(AsPath));
						}
						else
						{							
							tail = steps.AsPath(i + 2, steps.Length - i - 2);
							foreach (var dir in Directory.EnumerateDirectories(path, steps[i + 1], SearchOption.TopDirectoryOnly))
							{
								subPatterns.Enqueue(new Tuple<FileSystemPath, FileSystemPath>(dir.AsPath(), tail));
							}							

							tail = steps.AsPath(i, steps.Length - i);
							foreach (var dir in Directory.EnumerateDirectories(path))
							{
								subPatterns.Enqueue(new Tuple<FileSystemPath, FileSystemPath>(dir.AsPath(), tail));
							}
						}						
						break;
					}

					if (i + 1 == steps.Length || steps[i].Contains("*") || steps[i].Contains("?"))
					{
						var path = (pattern.Item1 / steps.AsPath(0, i)).Full;
						if (!Directory.Exists(path))
							break;

						if (i + 1 == steps.Length)
						{
							results.AddRange(enumerateEntries(path, steps[i], SearchOption.TopDirectoryOnly).Select(AsPath));
						}
						else
						{							
							tail = steps.AsPath(i + 1, steps.Length - i - 1);
							foreach (var dir in Directory.EnumerateDirectories(path, steps[i], SearchOption.TopDirectoryOnly))
							{
								subPatterns.Enqueue(new Tuple<FileSystemPath, FileSystemPath>(dir.AsPath(), tail));
							}
						}
						break;
					}
				}
			} while (subPatterns.Count > 0);

			return results;
		}

		private static void Delete(IEnumerable<FileSystemPath> pathes, Action<string> eraser)
		{
			var retry = 0;
			do
			{
				var deferred = new List<FileSystemPath>(0);

				foreach (var path in pathes)
				{
					try
					{
						Log.DebugFormat("DEL: {0}", path);			
						eraser(path.Full);
					}
					catch (IOException e)
					{
						Log.WarnFormat("DEL: Unable to delete file system entry. Operation deferred.\n  Path: {0}\n  Reason: {1}", path, e.Message);

						deferred.Add(path);
					}
				}

				if (deferred.Count == 0)
					return;

				Thread.Sleep(Defaults.RetryInterval);

				pathes = deferred;
			} while (++retry < Defaults.MaxRetries);

			throw new TargetFailureException(String.Format("Delete operation failed after {0} retries. Hint: see log for problem files/folders.", retry));
		}

		private static void FileEraser(string path)
		{
			if (!File.Exists(path))
				return;

			var attrs = File.GetAttributes(path);
			if ((attrs & Defaults.JunkFileAttributes) != 0)
			{
				File.SetAttributes(path, attrs & ~Defaults.JunkFileAttributes);
			}

			File.Delete(path);
		}

		private static void FolderEraser(string path)
		{
			if (!Directory.Exists(path))
				return;

			foreach (var file in Directory.EnumerateFiles(path, "*.*", SearchOption.AllDirectories))
			{
				FileEraser(file);
			}
			
			Directory.Delete(path, true);
		}
	}
}