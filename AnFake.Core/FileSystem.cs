using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
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
				throw new ArgumentNullException("path", "FileSystem.AsPath(path): path must not be null");

			return new FileSystemPath(path, false);
		}		

		public static FileSystemPath AsPath(this string[] pathSteps, int start, int count)
		{
			if (pathSteps == null)
				throw new ArgumentNullException("pathSteps", "FileSystem.AsPath(pathSteps): pathSteps must not be null");

			return new FileSystemPath(String.Join(DirectorySeparatorString, pathSteps, start, count), true);
		}

		public static FileItem AsFile(this FileSystemPath path)
		{
			if (path == null)
				throw new ArgumentNullException("path", "FileSystem.AsFile(path): path must not be null");

			return new FileItem(path, FileSystemPath.Base);
		}

		public static FileItem AsFile(this string path)
		{
			return AsFile(path.AsPath());
		}

		public static FileSet AsFileSet(this FileSystemPath wildcardedPath)
		{
			if (wildcardedPath == null)
				throw new ArgumentNullException("wildcardedPath", "FileSystem.AsFileSet(wildcardedPath): wildcardedPath must not be null");

			return new FileSet().Include(wildcardedPath);
		}

		public static FileSet AsFileSet(this string wildcardedPath)
		{
			return AsFileSet(wildcardedPath.AsPath());
		}

		public static FileSet AsFileSetFrom(this FileSystemPath wildcardedPath, FileSystemPath basePath)
		{
			if (wildcardedPath == null)
				throw new ArgumentNullException("wildcardedPath", "FileSystem.AsFileSetFrom(wildcardedPath, basePath): wildcardedPath must not be null");
			if (basePath == null)
				throw new ArgumentNullException("basePath", "FileSystem.AsFileSetFrom(wildcardedPath, basePath): basePath must not be null");

			return new FileSet().From(basePath)
				.Include(wildcardedPath);
		}

		public static FileSet AsFileSetFrom(this string wildcardedPath, string basePath)
		{
			return AsFileSetFrom(wildcardedPath.AsPath(), basePath.AsPath());
		}

		public static FolderItem AsFolder(this FileSystemPath path)
		{
			if (path == null)
				throw new ArgumentNullException("path", "FileSystem.AsFolder(path): path must not be null");

			return new FolderItem(path);
		}

		public static FolderItem AsFolder(this string path)
		{
			return AsFolder(path.AsPath());
		}

		public static FolderSet AsFolderSet(this FileSystemPath wildcardedPath)
		{
			if (wildcardedPath == null)
				throw new ArgumentNullException("wildcardedPath", "FileSystem.AsFolderSet(wildcardedPath): wildcardedPath must not be null");

			return new FolderSet().Include(wildcardedPath);
		}

		public static FolderSet AsFolderSet(this string wildcardedPath)
		{
			return AsFolderSet(wildcardedPath.AsPath());
		}

		public static void CopyTo(this IEnumerable<FileItem> files, FileSystemPath targetPath)
		{
			if (files == null)
				throw new ArgumentNullException("files", "FileSystem.CopyTo(files, targetPath): files must not be null");
			if (targetPath == null)
				throw new ArgumentNullException("targetPath", "FileSystem.CopyTo(files, targetPath): targetPath must not be null");

			CopyTo(files, targetPath, false);
		}

		public static void CopyTo(this IEnumerable<FileItem> files, FileSystemPath targetPath, bool overwrite)
		{
			if (files == null)
				throw new ArgumentNullException("files", "FileSystem.CopyTo(files, targetPath, overwrite): files must not be null");
			if (targetPath == null)
				throw new ArgumentNullException("targetPath", "FileSystem.Copy(files, targetPath, overwrite): targetPath must not be null");			

			var filesToCopy = files
				.Select(x => new Tuple<FileSystemPath, FileSystemPath>(x.Path, targetPath / x.RelPath))
				.ToArray();

			if (overwrite)
			{
				Log.DebugFormat("COPY: overwrite enabled => cleaning destination\n  TargetPath: {0}", targetPath);
				DeleteFiles(filesToCopy.Select(x => x.Item2));
			}

			foreach (var folder in filesToCopy.Select(x => x.Item2.Parent).Distinct())
			{
				Log.DebugFormat("COPY: creating destination folders\n  TargetPath: {0}", folder);
				Directory.CreateDirectory(folder.Full);
			}

			foreach (var file in filesToCopy)
			{
				Log.DebugFormat("COPY:\n  From: {0}\n    To: {1}", file.Item1, file.Item2);
				File.Copy(file.Item1.Full, file.Item2.Full);
			}

			Log.DebugFormat("COPY: total {0} files", filesToCopy.Length);
		}

		public static void CopyTo(this FileItem file, FileSystemPath targetPath)
		{
			if (file == null)
				throw new ArgumentNullException("file", "FileSystem.CopyTo(file, targetPath): file must not be null");
			if (targetPath == null)
				throw new ArgumentNullException("targetPath", "FileSystem.CopyTo(file, targetPath): targetPath must not be null");

			CopyTo(file, targetPath, false);
		}

		public static void CopyTo(FileItem file, FileSystemPath targetPath, bool overwrite)
		{
			if (file == null)
				throw new ArgumentNullException("file", "FileSystem.CopyTo(file, targetPath, overwrite): file must not be null");
			if (targetPath == null)
				throw new ArgumentNullException("targetPath", "FileSystem.CopyTo(file, targetPath, overwrite): targetPath must not be null");
			
			if (overwrite)
			{
				Log.DebugFormat("COPY: overwrite enabled => cleaning destination\n  TargetPath: {0}", targetPath);
				DeleteFile(targetPath);
			}

			var targetFolder = targetPath.Parent;
			Log.DebugFormat("COPY: creating destination folder\n  TargetPath: {0}", targetFolder);			
			Directory.CreateDirectory(targetFolder.Full);

			Log.DebugFormat("COPY:\n  From: {0}\n    To: {1}", file, targetPath);
			File.Copy(file.Path.Full, targetPath.Full);

			Log.Debug("COPY: total 1 file");
		}

		public static void Delete(this IEnumerable<FileItem> files)
		{
			if (files == null)
				throw new ArgumentNullException("files", "FileSystem.Delete(files): files must not be null");

			DeleteFiles(files.Select(x => x.Path));
		}

		public static void Delete(this FileItem file)
		{
			if (file == null)
				throw new ArgumentNullException("file", "FileSystem.Delete(file): file must not be null");

			DeleteFile(file.Path);
		}

		public static void Create(this FolderItem folder)
		{
			if (folder == null)
				throw new ArgumentNullException("folder", "FileSystem.Create(folder): folder must not be null");

			Directory.CreateDirectory(folder.Path.Full);
		}

		public static void Delete(this IEnumerable<FolderItem> folders)
		{
			if (folders == null)
				throw new ArgumentNullException("folders", "FileSystem.Delete(folders): folders must not be null");

			DeleteFolders(folders.Select(x => x.Path));
		}

		public static void Delete(this FolderItem folder)
		{
			if (folder == null)
				throw new ArgumentNullException("folder", "FileSystem.Delete(folder): folder must not be null");

			DeleteFolder(folder.Path);
		}

		public static void Clean(this IEnumerable<FolderItem> folders)
		{
			if (folders == null)
				throw new ArgumentNullException("folders", "FileSystem.Clean(folders): folders must not be null");

			var foldersToClean = folders.ToArray();

			Log.DebugFormat("CLEAN: deleting folders");
			DeleteFolders(foldersToClean.Select(x => x.Path));

			foreach (var folder in foldersToClean)
			{
				Log.DebugFormat("CLEAN: re-creating folder\n  TargetPath: {0}", folder);
				Directory.CreateDirectory(folder.Path.Full);
			}
		}

		public static void Clean(this FolderItem folder)
		{
			if (folder == null)
				throw new ArgumentNullException("folder", "FileSystem.Clean(folder): folder must not be null");

			Log.DebugFormat("CLEAN: deleting folder\n  TargetPath: {0}", folder);
			DeleteFolder(folder.Path);

			Log.DebugFormat("CLEAN: re-creating folder\n  TargetPath: {0}", folder);
			Directory.CreateDirectory(folder.Path.Full);
		}

		internal static IEnumerable<FileSystemPath> MatchFiles(FileSystemPath basePath, FileSystemPath wildcardedPath)
		{
			return Match(basePath, wildcardedPath, Directory.EnumerateFiles);
		}

		internal static IEnumerable<FileSystemPath> MatchFolders(FileSystemPath wildcardedPath)
		{
			return Match(FileSystemPath.Base, wildcardedPath, Directory.EnumerateDirectories);
		}

		private static void DeleteFiles(IEnumerable<FileSystemPath> files)
		{
			Delete(files, FileEraser);
		}

		private static void DeleteFile(FileSystemPath file)
		{
			Delete(new[] {file}, FileEraser);
		}

		private static void DeleteFolders(IEnumerable<FileSystemPath> folders)
		{
			Delete(folders, FolderEraser);
		}

		private static void DeleteFolder(FileSystemPath folder)
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