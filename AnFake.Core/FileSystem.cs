using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using AnFake.Api;
using AnFake.Core.Exceptions;

namespace AnFake.Core
{
	/// <summary>
	///		Represents basic tools related to file system. The most methods declared as extension methods.
	/// </summary>
	public static class FileSystem
	{
		private static readonly string DirectorySeparatorString = Path.DirectorySeparatorChar.ToString(CultureInfo.InvariantCulture);

		/// <summary>
		///		Generic parameters of file system operations.
		/// </summary>
		public sealed class Params
		{
			/// <summary>
			///		Max number of retries of delete operation.
			/// </summary>			
			public int MaxRetries;
			
			/// <summary>
			///		Delay between retries of delete operation.
			/// </summary>
			public TimeSpan RetryInterval;

			/// <summary>
			///		Set of file attributes to be removed before delete operation.
			/// </summary>
			public FileAttributes JunkFileAttributes;

			/// <summary>
			///		Constructs default parameters.
			/// </summary>
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

		/// <summary>
		///		Default parameters.
		/// </summary>
		public static Params Defaults { get; private set; }

		/// <summary>
		///		Converts string to FileSystemPath.
		/// </summary>
		/// <remarks>
		///		Relative path is evaluated against build script location.
		/// </remarks>
		/// <param name="path">path as plain string (not null)</param>
		/// <returns>FileSystemPath instance</returns>
		public static FileSystemPath AsPath(this string path)
		{
			if (path == null)
				throw new ArgumentException("FileSystem.AsPath(path): path must not be null");

			return new FileSystemPath(path, false);
		}		

		/// <summary>
		///		Converts splitted path in form of string array to FileSystemPath.
		/// </summary>
		/// <param name="pathSteps">array of path steps (not null)</param>
		/// <param name="start">index of first step</param>
		/// <param name="count">number of steps</param>
		/// <returns>FileSystemPath instance</returns>
		public static FileSystemPath AsPath(this string[] pathSteps, int start, int count)
		{
			if (pathSteps == null)
				throw new ArgumentException("FileSystem.AsPath(pathSteps): pathSteps must not be null");

			return new FileSystemPath(String.Join(DirectorySeparatorString, pathSteps, start, count), false);
		}

		/// <summary>
		///		Converts file path to FileItem.
		/// </summary>
		/// <param name="path">file path (not null)</param>
		/// <returns>FileItem instance</returns>
		public static FileItem AsFile(this FileSystemPath path)
		{
			if (path == null)
				throw new ArgumentException("FileSystem.AsFile(path): path must not be null");
			if (path.IsWildcarded)
				throw new ArgumentException("FileSystem.AsFile(path): path must not contain wildcard characters");

			return new FileItem(path, path.IsRooted ? path.Parent : FileSystemPath.Base);
		}

		/// <summary>
		///		Converts file path to FileItem.
		/// </summary>
		/// <param name="path">file path (not null)</param>
		/// <returns>FileItem instance</returns>
		public static FileItem AsFile(this string path)
		{
			if (path == null)
				throw new ArgumentException("FileSystem.AsFile(path): path must not be null");

			return AsFile(path.AsPath());
		}

		///  <summary>
		/// 		Converts file path to FileItem with specified base path.
		///  </summary>
		///  <param name="path">file path (not null)</param>
		/// <param name="basePath">base path (not null)</param>
		/// <returns>FileItem instance</returns>
		/// <seealso cref="FileItem.RelPath"/>
		public static FileItem AsFileFrom(this FileSystemPath path, FileSystemPath basePath)
		{
			if (path == null)
				throw new ArgumentException("FileSystem.AsFileFrom(path, basePath): path must not be null");
			if (path.IsWildcarded)
				throw new ArgumentException("FileSystem.AsFileFrom(path, basePath): path must not contain wildcard characters");
			if (basePath == null)
				throw new ArgumentException("FileSystem.AsFileFrom(path, basePath): basePath must not be null");
			if (basePath.IsWildcarded)
				throw new ArgumentException("FileSystem.AsFileFrom(path, basePath): basePath must not contain wildcard characters");

			return new FileItem(path, basePath);
		}

		///  <summary>
		/// 		Converts file path to FileItem with specified base path.
		///  </summary>
		///  <param name="path">file path (not null)</param>
		/// <param name="basePath">base path (not null)</param>
		/// <returns>FileItem instance</returns>
		/// <seealso cref="FileItem.RelPath"/>
		public static FileItem AsFileFrom(this FileSystemPath path, string basePath)
		{
			if (basePath == null)
				throw new ArgumentException("FileSystem.AsFileFrom(path, basePath): basePath must not be null");

			return AsFileFrom(path, basePath.AsPath());
		}

		///  <summary>
		/// 		Converts file path to FileItem with specified base path.
		///  </summary>
		///  <param name="path">file path (not null)</param>
		/// <param name="basePath">base path (not null)</param>
		/// <returns>FileItem instance</returns>
		/// <seealso cref="FileItem.RelPath"/>
		public static FileItem AsFileFrom(this string path, FileSystemPath basePath)
		{
			if (path == null)
				throw new ArgumentException("FileSystem.AsFileFrom(path, basePath): path must not be null");
			
			return AsFileFrom(path.AsPath(), basePath);
		}

		///  <summary>
		/// 		Converts file path to FileItem with specified base path.
		///  </summary>
		///  <param name="path">file path (not null)</param>
		/// <param name="basePath">base path (not null)</param>
		/// <returns>FileItem instance</returns>
		/// <seealso cref="FileItem.RelPath"/>
		public static FileItem AsFileFrom(this string path, string basePath)
		{
			if (path == null)
				throw new ArgumentException("FileSystem.AsFileFrom(path, basePath): path must not be null");
			if (basePath == null)
				throw new ArgumentException("FileSystem.AsFileFrom(path, basePath): basePath must not be null");

			return AsFileFrom(path.AsPath(), basePath.AsPath());
		}

		///  <summary>
		/// 		Converts wildcarded path to FileSet.
		///  </summary>		
		/// <param name="wildcardedPath">wildcarded path (not null)</param>
		/// <returns>FileSet instance</returns>		
		public static FileSet AsFileSet(this FileSystemPath wildcardedPath)
		{
			if (wildcardedPath == null)
				throw new ArgumentException("FileSystem.AsFileSet(wildcardedPath): wildcardedPath must not be null");

			return new FileSet().Include(wildcardedPath);
		}

		///  <summary>
		/// 		Converts array of wildcarded pathes to FileSet.
		///  </summary>		
		/// <param name="wildcardedPathes">array of wildcarded pathes (not null)</param>
		/// <returns>FileSet instance</returns>
		public static FileSet AsFileSet(this FileSystemPath[] wildcardedPathes)
		{
			if (wildcardedPathes == null)
				throw new ArgumentException("FileSystem.AsFileSet(wildcardedPathes): wildcardedPathes must not be null");

			var fs = new FileSet();
			foreach (var path in wildcardedPathes)
			{
				fs.Include(path);
			}

			return fs;
		}

		///  <summary>
		/// 		Converts wildcarded path to FileSet.
		///  </summary>		
		/// <param name="wildcardedPath">wildcarded path (not null)</param>
		/// <returns>FileSet instance</returns>
		public static FileSet AsFileSet(this string wildcardedPath)
		{
			if (wildcardedPath == null)
				throw new ArgumentException("FileSystem.AsFileSet(wildcardedPath): wildcardedPath must not be null");

			return AsFileSet(wildcardedPath.AsPath());
		}

		///  <summary>
		/// 		Converts array of wildcarded pathes to FileSet.
		///  </summary>		
		/// <param name="wildcardedPathes">array of wildcarded pathes (not null)</param>
		/// <returns>FileSet instance</returns>
		public static FileSet AsFileSet(this string[] wildcardedPathes)
		{
			if (wildcardedPathes == null)
				throw new ArgumentException("FileSystem.AsFileSet(wildcardedPathes): wildcardedPathes must not be null");

			var fs = new FileSet();
			foreach (var path in wildcardedPathes)
			{
				fs.Include(path);
			}

			return fs;
		}

		///  <summary>
		/// 		Converts wildcarded path to FileSet with specified base path.
		///  </summary>		
		/// <param name="wildcardedPath">wildcarded path (not null)</param>
		/// <param name="basePath">base path (not null)</param>
		/// <returns>FileSet instance</returns>
		/// <seealso cref="FileItem.RelPath"/>
		public static FileSet AsFileSetFrom(this FileSystemPath wildcardedPath, FileSystemPath basePath)
		{
			if (wildcardedPath == null)
				throw new ArgumentException("FileSystem.AsFileSetFrom(wildcardedPath, basePath): wildcardedPath must not be null");
			if (basePath == null)
				throw new ArgumentException("FileSystem.AsFileSetFrom(wildcardedPath, basePath): basePath must not be null");

			return new FileSet().From(basePath)
				.Include(wildcardedPath);
		}

		///  <summary>
		/// 		Converts wildcarded path to FileSet with specified base path.
		///  </summary>		
		/// <param name="wildcardedPath">wildcarded path (not null)</param>
		/// <param name="basePath">base path (not null)</param>
		/// <returns>FileSet instance</returns>
		/// <seealso cref="FileItem.RelPath"/>
		public static FileSet AsFileSetFrom(this FileSystemPath wildcardedPath, string basePath)
		{
			if (wildcardedPath == null)
				throw new ArgumentException("FileSystem.AsFileSetFrom(wildcardedPath, basePath): wildcardedPath must not be null");
			if (basePath == null)
				throw new ArgumentException("FileSystem.AsFileSetFrom(wildcardedPath, basePath): basePath must not be null");

			return AsFileSetFrom(wildcardedPath, basePath.AsPath());
		}

		///  <summary>
		/// 		Converts wildcarded path to FileSet with specified base path.
		///  </summary>		
		/// <param name="wildcardedPath">wildcarded path (not null)</param>
		/// <param name="basePath">base path (not null)</param>
		/// <returns>FileSet instance</returns>
		/// <seealso cref="FileItem.RelPath"/>
		public static FileSet AsFileSetFrom(this string wildcardedPath, FileSystemPath basePath)
		{
			if (wildcardedPath == null)
				throw new ArgumentException("FileSystem.AsFileSetFrom(wildcardedPath, basePath): wildcardedPath must not be null");
			if (basePath == null)
				throw new ArgumentException("FileSystem.AsFileSetFrom(wildcardedPath, basePath): basePath must not be null");

			return AsFileSetFrom(wildcardedPath.AsPath(), basePath);
		}

		///  <summary>
		/// 		Converts wildcarded path to FileSet with specified base path.
		///  </summary>		
		/// <param name="wildcardedPath">wildcarded path (not null)</param>
		/// <param name="basePath">base path (not null)</param>
		/// <returns>FileSet instance</returns>
		/// <seealso cref="FileItem.RelPath"/>
		public static FileSet AsFileSetFrom(this string wildcardedPath, string basePath)
		{
			if (wildcardedPath == null)
				throw new ArgumentException("FileSystem.AsFileSetFrom(wildcardedPath, basePath): wildcardedPath must not be null");
			if (basePath == null)
				throw new ArgumentException("FileSystem.AsFileSetFrom(wildcardedPath, basePath): basePath must not be null");

			return AsFileSetFrom(wildcardedPath.AsPath(), basePath.AsPath());
		}

		///  <summary>
		/// 		Converts folder path to FolderItem.
		///  </summary>
		/// <param name="path">folder path (not null)</param>
		/// <returns>FolderItem instance</returns>
		public static FolderItem AsFolder(this FileSystemPath path)
		{
			if (path == null)
				throw new ArgumentException("FileSystem.AsFolder(path): path must not be null");
			if (path.IsWildcarded)
				throw new ArgumentException("FileSystem.AsFolder(path): path must not contain wildcard characters");

			return new FolderItem(path);
		}

		///  <summary>
		/// 		Converts folder path to FolderItem.
		///  </summary>
		/// <param name="path">folder path (not null)</param>
		/// <returns>FolderItem instance</returns>
		public static FolderItem AsFolder(this string path)
		{
			if (path == null)
				throw new ArgumentException("FileSystem.AsFolder(path): path must not be null");

			return AsFolder(path.AsPath());
		}

		///  <summary>
		/// 		Converts wildcarded path to FolderSet.
		///  </summary>		
		/// <param name="wildcardedPath">wildcarded path (not null)</param>
		/// <returns>FolderSet instance</returns>
		public static FolderSet AsFolderSet(this FileSystemPath wildcardedPath)
		{
			if (wildcardedPath == null)
				throw new ArgumentException("FileSystem.AsFolderSet(wildcardedPath): wildcardedPath must not be null");

			return new FolderSet().Include(wildcardedPath);
		}

		///  <summary>
		/// 		Converts wildcarded path to FolderSet.
		///  </summary>		
		/// <param name="wildcardedPath">wildcarded path (not null)</param>
		/// <returns>FolderSet instance</returns>
		public static FolderSet AsFolderSet(this string wildcardedPath)
		{
			if (wildcardedPath == null)
				throw new ArgumentException("FileSystem.AsFolderSet(wildcardedPath): wildcardedPath must not be null");

			return AsFolderSet(wildcardedPath.AsPath());
		}
		
		/// <summary>
		///		Generates unique name by adding formatted timestamp to given base name.
		/// </summary>
		/// <param name="name">base name (not null)</param>
		/// <param name="ext">extension, including dot (not null)</param>
		/// <returns>unique name with extension</returns>
		public static string MakeUnique(this string name, string ext)
		{
			if (name == null)
				throw new ArgumentException("FileSystem.MakeUnique(name, ext): name must not be null");
			if (ext == null)
				throw new ArgumentException("FileSystem.MakeUnique(name, ext): ext must not be null");

			return String.Format("{0}.{1:yyyyMMdd.HHmmss.ff}{2}", name, DateTime.Now, ext);
		}

		/// <summary>
		///		Generates unique name. Equals to <see cref="MakeUnique(string,string)">MakeUnique(name, "")</see>.
		/// </summary>
		/// <param name="name">base name (not null)</param>
		/// <returns>unique name</returns>
		public static string MakeUnique(this string name)
		{
			if (name == null)
				throw new ArgumentException("FileSystem.MakeUnique(name): name must not be null");

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

						if (i + 1 == steps.Length)
						{
							results.AddRange(enumerateEntries(path, "*", SearchOption.AllDirectories).Select(AsPath));
						}
						else
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
						Trace.DebugFormat("FileSystem.Delete: {0}", path);
						eraser(path.Full);
					}
					catch (Exception e)
					{
						if (!(e is IOException) && !(e is UnauthorizedAccessException))
							throw;

						Trace.DebugFormat("FileSystem.Delete: Unable to delete file system entry. Operation deferred.\n  Path: {0}\n  Reason: {1}", path, e.Message);

						deferred.Add(path);
					}

					Interruption.CheckPoint();
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