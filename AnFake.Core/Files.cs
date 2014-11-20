using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AnFake.Core.Exceptions;
using Common.Logging;

namespace AnFake.Core
{
	public static class Files
	{
		private static readonly ILog Log = LogManager.GetLogger(typeof(Files).FullName);

		public static void Copy(IEnumerable<FileItem> files, FileSystemPath targetPath)
		{
			if (files == null)
				throw new AnFakeArgumentException("Files.Copy(files, targetPath): files must not be null");
			if (targetPath == null)
				throw new AnFakeArgumentException("Files.Copy(files, targetPath): targetPath must not be null");

			Copy(files, targetPath, false);
		}

		public static void Copy(IEnumerable<FileItem> files, FileSystemPath targetPath, bool overwrite)
		{
			if (files == null)
				throw new AnFakeArgumentException("Files.Copy(files, targetPath, overwrite): files must not be null");
			if (targetPath == null)
				throw new AnFakeArgumentException("Files.Copy(files, targetPath, overwrite): targetPath must not be null");

			var filesToCopy = files
				.Select(x => new Tuple<FileSystemPath, FileSystemPath>(x.Path, targetPath / x.RelPath))
				.ToArray();

			if (overwrite)
			{
				Log.DebugFormat("COPY: overwrite enabled => cleaning destination\n  TargetPath: {0}", targetPath);
				FileSystem.DeleteFiles(filesToCopy.Select(x => x.Item2));
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

		public static void Copy(FileItem file, FileSystemPath targetPath)
		{
			if (file == null)
				throw new AnFakeArgumentException("Files.Copy(file, targetPath): file must not be null");
			if (targetPath == null)
				throw new AnFakeArgumentException("Files.Copy(file, targetPath): targetPath must not be null");

			Copy(file, targetPath, false);
		}

		public static void Copy(string filePath, string targetPath)
		{
			if (filePath == null)
				throw new AnFakeArgumentException("Files.Copy(filePath, targetPath): filePath must not be null");
			if (targetPath == null)
				throw new AnFakeArgumentException("Files.Copy(filePath, targetPath): targetPath must not be null");

			Copy(filePath.AsFile(), targetPath.AsPath(), false);
		}

		public static void Copy(FileItem file, FileSystemPath targetPath, bool overwrite)
		{
			if (file == null)
				throw new AnFakeArgumentException("Files.Copy(file, targetPath, overwrite): file must not be null");
			if (targetPath == null)
				throw new AnFakeArgumentException("Files.Copy(file, targetPath, overwrite): targetPath must not be null");

			if (overwrite)
			{
				Log.DebugFormat("COPY: overwrite enabled => cleaning destination\n  TargetPath: {0}", targetPath);
				FileSystem.DeleteFile(targetPath);
			}

			var targetFolder = targetPath.Parent;
			Log.DebugFormat("COPY: creating destination folder\n  TargetPath: {0}", targetFolder);
			Directory.CreateDirectory(targetFolder.Full);

			Log.DebugFormat("COPY:\n  From: {0}\n    To: {1}", file, targetPath);
			File.Copy(file.Path.Full, targetPath.Full);

			Log.Debug("COPY: total 1 file");
		}

		public static void Copy(string filePath, string targetPath, bool overwrite)
		{
			if (filePath == null)
				throw new AnFakeArgumentException("Files.Copy(filePath, targetPath, overwrite): filePath must not be null");
			if (targetPath == null)
				throw new AnFakeArgumentException("Files.Copy(filePath, targetPath, overwrite): targetPath must not be null");

			Copy(filePath.AsFile(), targetPath.AsPath(), overwrite);
		}

		public static void Delete(IEnumerable<FileItem> files)
		{
			if (files == null)
				throw new AnFakeArgumentException("Files.Delete(files): files must not be null");

			FileSystem.DeleteFiles(files.Select(x => x.Path));
		}

		public static void Delete(FileItem file)
		{
			if (file == null)
				throw new AnFakeArgumentException("Files.Delete(file): file must not be null");

			FileSystem.DeleteFile(file.Path);
		}

		public static void Delete(FileSystemPath path)
		{
			if (path == null)
				throw new AnFakeArgumentException("Files.Delete(path): path must not be null");

			Delete(path.AsFile());
		}

		public static void Delete(string path)
		{
			if (path == null)
				throw new AnFakeArgumentException("Files.Delete(path): path must not be null");

			Delete(path.AsFile());
		}

		public static Snapshot Snapshot(IEnumerable<FileItem> files)
		{
			if (files == null)
				throw new AnFakeArgumentException("Files.Snapshot(files): files must not be null");

			var snapshot = new Snapshot();			
			try
			{
				foreach (var file in files)
				{
					snapshot.Save(file);
				}
			}
			catch (Exception)
			{
				snapshot.Dispose();
				throw;
			}
			
			return snapshot;
		}
	}
}
