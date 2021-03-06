﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AnFake.Api;

namespace AnFake.Core
{
	/// <summary>
	///		Files related tools.
	/// </summary>
	public static class Files
	{
		/// <summary>
		///		Copies given files to specified target folder.
		/// </summary>
		/// <param name="files">files to be copied (not null)</param>
		/// <param name="targetPath">path to target folder (not null)</param>
		/// <param name="overwrite">overwrite existing files?</param>
		public static void Copy(IEnumerable<FileItem> files, FileSystemPath targetPath, bool overwrite = false)
		{
			if (files == null)
				throw new ArgumentException("Files.Copy(files, targetPath[, overwrite]): files must not be null");
			if (targetPath == null)
				throw new ArgumentException("Files.Copy(files, targetPath[, overwrite]): targetPath must not be null");

			files = files.AsFormattable();

			Trace.InfoFormat("Copying files {{{0}}} to '{1}'...", files.ToFormattedString(), targetPath);

			var filePathes = files
				.Select(x => new Tuple<FileSystemPath, FileSystemPath>(x.Path, targetPath / x.RelPath))
				.ToArray();

			PrepareDestination(filePathes.Select(x => x.Item2), overwrite);

			foreach (var file in filePathes)
			{
				Trace.DebugFormat("Files.Copy:\n  From: {0}\n    To: {1}", file.Item1, file.Item2);
				File.Copy(file.Item1.Full, file.Item2.Full);

				Interruption.CheckPoint();
			}

			Trace.InfoFormat("{0} file(s) copied.", filePathes.Length);
		}

		public static void Copy(FileItem file, FileSystemPath targetFilePath, bool overwrite = false)
		{
			if (file == null)
				throw new ArgumentException("Files.Copy(file, targetFilePath[, overwrite]): file must not be null");
			if (targetFilePath == null)
				throw new ArgumentException("Files.Copy(file, targetFilePath[, overwrite]): targetFilePath must not be null");

			Copy(file.Path, targetFilePath, overwrite);
		}

		public static void Copy(FileSystemPath sourceFilePath, FileSystemPath targetFilePath, bool overwrite = false)
		{
			if (sourceFilePath == null)
				throw new ArgumentException("Files.Copy(sourceFilePath, targetFilePath[, overwrite]): sourceFilePath must not be null");
			if (targetFilePath == null)
				throw new ArgumentException("Files.Copy(sourceFilePath, targetFilePath[, overwrite]): targetFilePath must not be null");

			Trace.InfoFormat("Copying '{0}' to '{1}'...", sourceFilePath, targetFilePath);

			PrepareDestination(new[] {targetFilePath}, overwrite);

			Trace.DebugFormat("Files.Copy:\n  From: {0}\n    To: {1}", sourceFilePath, targetFilePath);
			File.Copy(sourceFilePath.Full, targetFilePath.Full);
		}

		public static void Copy(string sourceFilePath, string targetFilePath, bool overwrite = false)
		{
			if (sourceFilePath == null)
				throw new ArgumentException("Files.Copy(sourceFilePath, targetFilePath, overwrite): sourceFilePath must not be null");
			if (targetFilePath == null)
				throw new ArgumentException("Files.Copy(sourceFilePath, targetFilePath, overwrite): targetFilePath must not be null");

			Copy(sourceFilePath.AsPath(), targetFilePath.AsPath(), overwrite);
		}

		public static void Move(IEnumerable<FileItem> files, FileSystemPath targetPath, bool overwrite = false)
		{
			if (files == null)
				throw new ArgumentException("Files.Move(files, targetPath[, overwrite]): files must not be null");
			if (targetPath == null)
				throw new ArgumentException("Files.Move(files, targetPath[, overwrite]): targetPath must not be null");

			files = files.AsFormattable();

			Trace.InfoFormat("Moving files {{{0}}} to '{1}'...", files.ToFormattedString(), targetPath);

			var filePathes = files
				.Select(x => new Tuple<FileSystemPath, FileSystemPath>(x.Path, targetPath / x.RelPath))
				.ToArray();

			PrepareDestination(filePathes.Select(x => x.Item2), overwrite);
			
			foreach (var file in filePathes)
			{
				Trace.DebugFormat("Files.Move:\n  From: {0}\n    To: {1}", file.Item1, file.Item2);
				File.Move(file.Item1.Full, file.Item2.Full);

				Interruption.CheckPoint();
			}

			Trace.InfoFormat("{0} file(s) moved.", filePathes.Length);
		}

		public static void Move(FileItem file, FileSystemPath targetFilePath, bool overwrite = false)
		{
			if (file == null)
				throw new ArgumentException("Files.Move(file, targetFilePath[, overwrite]): file must not be null");
			if (targetFilePath == null)
				throw new ArgumentException("Files.Move(file, targetFilePath[, overwrite]): targetFilePath must not be null");

			Move(file.Path, targetFilePath, overwrite);
		}
		
		public static void Move(FileSystemPath sourceFilePath, FileSystemPath targetFilePath, bool overwrite = false)
		{
			if (sourceFilePath == null)
				throw new ArgumentException("Files.Move(sourceFilePath, targetFilePath[, overwrite]): sourceFilePath must not be null");
			if (targetFilePath == null)
				throw new ArgumentException("Files.Move(sourceFilePath, targetFilePath[, overwrite]): targetFilePath must not be null");

			Trace.InfoFormat("Moving '{0}' to '{1}'...", sourceFilePath, targetFilePath);

			PrepareDestination(new[] { targetFilePath }, overwrite);

			Trace.DebugFormat("Files.Move:\n  From: {0}\n    To: {1}", sourceFilePath, targetFilePath);
			File.Move(sourceFilePath.Full, targetFilePath.Full);
		}

		public static void Move(string sourceFilePath, string targetFilePath, bool overwrite = false)
		{
			if (sourceFilePath == null)
				throw new ArgumentException("Files.Move(sourceFilePath, targetFilePath, overwrite): sourceFilePath must not be null");
			if (targetFilePath == null)
				throw new ArgumentException("Files.Move(sourceFilePath, targetFilePath, overwrite): targetFilePath must not be null");

			Move(sourceFilePath.AsPath(), targetFilePath.AsPath(), overwrite);
		}

		public static void Delete(IEnumerable<FileItem> files)
		{
			if (files == null)
				throw new ArgumentException("Files.Delete(files): files must not be null");

			files = files.AsFormattable();

			Trace.InfoFormat("Deleting files {{{0}}}...", files.ToFormattedString());

			var filePathes = files
				.Select(x => x.Path)
				.ToArray();

			FileSystem.DeleteFiles(filePathes);

			Trace.InfoFormat("{0} file(s) deleted.", filePathes.Length);
		}

		public static void Delete(FileItem file)
		{
			if (file == null)
				throw new ArgumentException("Files.Delete(file): file must not be null");

			Delete(file.Path);
		}

		public static void Delete(FileSystemPath filePath)
		{
			if (filePath == null)
				throw new ArgumentException("Files.Delete(filePath): filePath must not be null");

			Trace.InfoFormat("Deleting '{0}'...", filePath);

			FileSystem.DeleteFile(filePath);
		}
		
		public static void Delete(string filePath)
		{
			if (filePath == null)
				throw new ArgumentException("Files.Delete(filePath): filePath must not be null");

			Delete(filePath.AsPath());
		}

		public static Snapshot Snapshot(IEnumerable<FileItem> files)
		{
			if (files == null)
				throw new ArgumentException("Files.Snapshot(files): files must not be null");

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

		private static void PrepareDestination(IEnumerable<FileSystemPath> destinationPathes, bool overwrite)
		{
			var destinationPathesArray = destinationPathes.ToArray();

			if (overwrite)
			{
				Trace.Debug("Files.Copy/Move: overwrite enabled => cleaning destination");
				FileSystem.DeleteFiles(destinationPathesArray);
			}

			foreach (var folder in destinationPathesArray.Select(x => x.Parent).Distinct())
			{
				Trace.DebugFormat("Files.Copy/Move: creating destination folders\n  TargetPath: {0}", folder);
				Directory.CreateDirectory(folder.Full);
			}
		}
	}
}
