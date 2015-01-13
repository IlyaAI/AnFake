using System;
using System.Collections.Generic;
using System.IO;
using AnFake.Api;
using AnFake.Core.Exceptions;

namespace AnFake.Core
{
	public sealed class Snapshot : IDisposable
	{
		public sealed class SavedFile
		{
			public readonly FileSystemPath Path;
			public readonly DateTime LastModified;			
			public readonly FileAttributes Attributes;

			public SavedFile(FileSystemPath path, DateTime lastModified, FileAttributes attributes)
			{
				Path = path;
				LastModified = lastModified;				
				Attributes = attributes;
			}
		}

		private readonly IList<SavedFile> _originalFiles = new List<SavedFile>();
		private readonly string _snapshotBasePath;

		public Snapshot()
		{
			_snapshotBasePath = Path.Combine(Path.GetTempPath(), "AnFake".MakeUnique());
			Directory.CreateDirectory(_snapshotBasePath);
		}

		public void Save(FileSystemPath filePath)
		{			
			var fullPath = filePath.Full;
			var snapshotPath = Path.Combine(_snapshotBasePath, String.Format("{0:X8}", _originalFiles.Count));
			var fi = new FileInfo(fullPath);
			var lastModified = fi.LastWriteTimeUtc;			
			var attributes = fi.Attributes;

			Trace.DebugFormat("Snapshot.Save: {0} => {1}", fullPath, snapshotPath);
			fi.CopyTo(snapshotPath);

			var fs = new SavedFile(filePath, lastModified, attributes);

			_originalFiles.Add(fs);

			if (FileSaved != null)
			{
				FileSaved.Invoke(this, fs);
			}
		}

		public void Revert()
		{
			for (var index = 0; index < _originalFiles.Count; index++)
			{
				try
				{
					var originalFile = _originalFiles[index];
					var snapshotPath = Path.Combine(_snapshotBasePath, String.Format("{0:X8}", index));

					FileSystem.DeleteFile(originalFile.Path);
					
					var fi = new FileInfo(snapshotPath);
					fi.MoveTo(originalFile.Path.Full);					
					fi.Attributes = originalFile.Attributes;
					// It isn't neccessary to restore LastWriteTime manually, becasue is's preserved by CopyTo/MoveTo function.

					if (FileReverted != null)
					{
						FileReverted.Invoke(this, originalFile);
					}
				}
				catch (Exception e)
				{
					Trace.WarnFormat("Snapshot.Revert: {0}", AnFakeException.ToString(e));
				}
			}

			Cleanup();
		}

		public void Dispose()
		{
			Cleanup();			
		}

		public static event EventHandler<SavedFile> FileSaved;
		public static event EventHandler<SavedFile> FileReverted;		

		private void Cleanup()
		{
			_originalFiles.Clear();

			if (!Directory.Exists(_snapshotBasePath))
				return;

			try
			{
				FileSystem.DeleteFolder(_snapshotBasePath.AsPath());
			}
			catch (Exception e)
			{
				Trace.WarnFormat("Snapshot.Cleanup: {0}", AnFakeException.ToString(e));
			}
		}
	}
}