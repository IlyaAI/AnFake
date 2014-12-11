using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AnFake.Api;

namespace AnFake.Core
{
	public static class Folders
	{
		public static FolderItem Current
		{
			get { return Directory.GetCurrentDirectory().AsFolder(); }
		}

		public static void Create(FileSystemPath folderPath)
		{
			if (folderPath == null)
				throw new ArgumentException("Folders.Create(folderPath): folderPath must not be null");

			Trace.InfoFormat("Creating '{0}'...", folderPath);

			Directory.CreateDirectory(folderPath.Full);
		}

		public static void Create(string folderPath)
		{
			if (folderPath == null)
				throw new ArgumentException("Folders.Create(folderPath): folderPath must not be null");

			Create(folderPath.AsPath());
		}

		public static void Delete(IEnumerable<FolderItem> folders)
		{
			if (folders == null)
				throw new ArgumentException("Folders.Delete(folders): folders must not be null");

			Trace.InfoFormat("Deleting folders...");

			var folderPathes = folders
				.Select(x => x.Path)
				.ToArray();

			FileSystem.DeleteFolders(folderPathes);

			Trace.InfoFormat("{0} folder(s) deleted.", folderPathes.Length);
		}

		public static void Delete(FolderItem folder)
		{
			if (folder == null)
				throw new ArgumentException("Folders.Delete(folder): folder must not be null");

			Delete(folder.Path);
		}

		public static void Delete(FileSystemPath folderPath)
		{
			if (folderPath == null)
				throw new ArgumentException("Folders.Delete(folderPath): folderPath must not be null");

			Trace.InfoFormat("Deleting '{0}'...", folderPath);

			FileSystem.DeleteFolder(folderPath);
		}

		public static void Delete(string folderPath)
		{
			if (folderPath == null)
				throw new ArgumentException("Folders.Delete(folderPath): folderPath must not be null");

			Delete(folderPath.AsPath());
		}

		public static void Clean(IEnumerable<FolderItem> folders)
		{
			if (folders == null)
				throw new ArgumentException("Folders.Clean(folders): folders must not be null");

			Trace.Info("Cleaning folders...");

			var folderPathes = folders.ToArray();

			Trace.DebugFormat("Folders.Clean: deleting folders");
			FileSystem.DeleteFolders(folderPathes.Select(x => x.Path));

			foreach (var folder in folderPathes)
			{
				Trace.DebugFormat("Folders.Clean: re-creating folder\n  TargetPath: {0}", folder);
				Directory.CreateDirectory(folder.Path.Full);
			}

			Trace.InfoFormat("{0} folders cleaned.", folderPathes.Length);
		}

		public static void Clean(FolderItem folder)
		{
			if (folder == null)
				throw new ArgumentException("Folders.Clean(folder): folder must not be null");

			Clean(folder.Path);
		}

		public static void Clean(FileSystemPath folderPath)
		{
			if (folderPath == null)
				throw new ArgumentException("Folders.Clean(folderPath): folderPath must not be null");

			Trace.InfoFormat("Cleaning '{0}'...", folderPath);

			Trace.DebugFormat("Folders.Clean: deleting folder\n  TargetPath: {0}", folderPath);
			FileSystem.DeleteFolder(folderPath);

			Trace.DebugFormat("Folders.Clean: re-creating folder\n  TargetPath: {0}", folderPath);
			Directory.CreateDirectory(folderPath.Full);
		}

		public static void Clean(string path)
		{
			if (path == null)
				throw new ArgumentException("Folders.Clean(path): path must not be null");

			Clean(path.AsPath());
		}
	}
}