using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AnFake.Core.Exceptions;
using Common.Logging;

namespace AnFake.Core
{
	public static class Folders
	{
		private static readonly ILog Log = LogManager.GetLogger(typeof(Folders).FullName);

		public static FolderItem Current
		{
			get { return Directory.GetCurrentDirectory().AsFolder(); }
		}

		public static void Create(FolderItem folder)
		{
			if (folder == null)
				throw new AnFakeArgumentException("Folders.Create(folder): folder must not be null");

			Directory.CreateDirectory(folder.Path.Full);
		}

		public static void Create(FileSystemPath path)
		{
			if (path == null)
				throw new AnFakeArgumentException("Folders.Create(path): path must not be null");

			Create(path.AsFolder());
		}

		public static void Create(string path)
		{
			if (path == null)
				throw new AnFakeArgumentException("Folders.Create(path): path must not be null");

			Create(path.AsFolder());
		}

		public static void Delete(IEnumerable<FolderItem> folders)
		{
			if (folders == null)
				throw new AnFakeArgumentException("Folders.Delete(folders): folders must not be null");

			FileSystem.DeleteFolders(folders.Select(x => x.Path));
		}

		public static void Delete(FolderItem folder)
		{
			if (folder == null)
				throw new AnFakeArgumentException("Folders.Delete(folder): folder must not be null");

			FileSystem.DeleteFolder(folder.Path);
		}

		public static void Delete(FileSystemPath path)
		{
			if (path == null)
				throw new AnFakeArgumentException("Folders.Delete(path): path must not be null");

			Delete(path.AsFolder());
		}

		public static void Delete(string path)
		{
			if (path == null)
				throw new AnFakeArgumentException("Folders.Delete(path): path must not be null");

			Delete(path.AsFolder());
		}

		public static void Clean(IEnumerable<FolderItem> folders)
		{
			if (folders == null)
				throw new AnFakeArgumentException("Folders.Clean(folders): folders must not be null");

			var foldersToClean = folders.ToArray();

			Log.DebugFormat("CLEAN: deleting folders");
			FileSystem.DeleteFolders(foldersToClean.Select(x => x.Path));

			foreach (var folder in foldersToClean)
			{
				Log.DebugFormat("CLEAN: re-creating folder\n  TargetPath: {0}", folder);
				Directory.CreateDirectory(folder.Path.Full);
			}
		}

		public static void Clean(FolderItem folder)
		{
			if (folder == null)
				throw new AnFakeArgumentException("Folders.Clean(folder): folder must not be null");

			Log.DebugFormat("CLEAN: deleting folder\n  TargetPath: {0}", folder);
			FileSystem.DeleteFolder(folder.Path);

			Log.DebugFormat("CLEAN: re-creating folder\n  TargetPath: {0}", folder);
			Directory.CreateDirectory(folder.Path.Full);
		}

		public static void Clean(FileSystemPath path)
		{
			if (path == null)
				throw new AnFakeArgumentException("Folders.Clean(path): path must not be null");

			Clean(path.AsFolder());
		}

		public static void Clean(string path)
		{
			if (path == null)
				throw new AnFakeArgumentException("Folders.Clean(path): path must not be null");

			Clean(path.AsFolder());
		}
	}
}
