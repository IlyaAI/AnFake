using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AnFake.Api;
using AnFake.Core.Exceptions;

namespace AnFake.Core
{
	public static class Files
	{		
		public static void Copy(IEnumerable<FileItem> files, FileSystemPath targetPath)
		{
			Copy(files, targetPath, false);
		}

		public static void Copy(IEnumerable<FileItem> files, FileSystemPath targetPath, bool overwrite)
		{
			if (files == null)
				throw new AnFakeArgumentException("Files.Copy(files, targetPath[, overwrite]): files must not be null");
			if (targetPath == null)
				throw new AnFakeArgumentException("Files.Copy(files, targetPath[, overwrite]): targetPath must not be null");

			Trace.InfoFormat("Copying files to '{0}'...", targetPath);

			var filesToCopy = files
				.Select(x => new Tuple<FileSystemPath, FileSystemPath>(x.Path, targetPath / x.RelPath))
				.ToArray();

			if (overwrite)
			{
				Trace.DebugFormat("Files.Copy: overwrite enabled => cleaning destination\n  TargetPath: {0}", targetPath);
				FileSystem.DeleteFiles(filesToCopy.Select(x => x.Item2));
			}

			foreach (var folder in filesToCopy.Select(x => x.Item2.Parent).Distinct())
			{
				Trace.DebugFormat("Files.Copy: creating destination folders\n  TargetPath: {0}", folder);
				Directory.CreateDirectory(folder.Full);
			}

			foreach (var file in filesToCopy)
			{
				Trace.DebugFormat("Files.Copy:\n  From: {0}\n    To: {1}", file.Item1, file.Item2);
				File.Copy(file.Item1.Full, file.Item2.Full);
			}

			Trace.InfoFormat("{0} file(s) copied.", filesToCopy.Length);
		}

		public static void Copy(FileSystemPath filePath, FileSystemPath targetPath)
		{
			Copy(filePath, targetPath, false);
		}

		public static void Copy(string filePath, string targetPath)
		{
			if (filePath == null)
				throw new AnFakeArgumentException("Files.Copy(filePath, targetPath): filePath must not be null");
			if (targetPath == null)
				throw new AnFakeArgumentException("Files.Copy(filePath, targetPath): targetPath must not be null");

			Copy(filePath.AsPath(), targetPath.AsPath(), false);
		}

		public static void Copy(FileSystemPath filePath, FileSystemPath targetPath, bool overwrite)
		{
			if (filePath == null)
				throw new AnFakeArgumentException("Files.Copy(filePath, targetPath[, overwrite]): filePath must not be null");
			if (targetPath == null)
				throw new AnFakeArgumentException("Files.Copy(filePath, targetPath[, overwrite]): targetPath must not be null");

			Trace.InfoFormat("Copying '{0}' to '{1}'...", filePath, targetPath);

			if (overwrite)
			{
				Trace.DebugFormat("Files.Copy: overwrite enabled => cleaning destination\n  TargetPath: {0}", targetPath);
				FileSystem.DeleteFile(targetPath);
			}

			var targetFolder = targetPath.Parent;
			Trace.DebugFormat("Files.Copy: creating destination folder\n  TargetPath: {0}", targetFolder);
			Directory.CreateDirectory(targetFolder.Full);

			Trace.DebugFormat("Files.Copy:\n  From: {0}\n    To: {1}", filePath, targetPath);
			File.Copy(filePath.Full, targetPath.Full);			
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

			Trace.Info("Deleting files...");

			var filePathes = files
				.Select(x => x.Path)
				.ToArray();

			FileSystem.DeleteFiles(filePathes);

			Trace.InfoFormat("{0} file(s) deleted.");
		}

		public static void Delete(FileSystemPath filePath)
		{
			if (filePath == null)
				throw new AnFakeArgumentException("Files.Delete(filePath): filePath must not be null");

			Trace.InfoFormat("Deleting '{0}'...", filePath);

			FileSystem.DeleteFile(filePath);
		}
		
		public static void Delete(string path)
		{
			if (path == null)
				throw new AnFakeArgumentException("Files.Delete(path): path must not be null");

			Delete(path.AsPath());
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
