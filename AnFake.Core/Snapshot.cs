using System;
using System.Collections.Generic;
using System.IO;
using AnFake.Api;

namespace AnFake.Core
{
	public sealed class Snapshot : IDisposable
	{
		class SnapshotInfo
		{
			public readonly string Path;
			public readonly DateTime LastModified;			
			public readonly FileAttributes Attributes;

			public SnapshotInfo(string path, DateTime lastModified, FileAttributes attributes)
			{
				Path = path;
				LastModified = lastModified;				
				Attributes = attributes;
			}
		}

		private readonly IList<SnapshotInfo> _originalFiles = new List<SnapshotInfo>();
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

			_originalFiles.Add(new SnapshotInfo(fullPath, lastModified, attributes));
		}

		public void Revert()
		{
			for (var index = 0; index < _originalFiles.Count; index++)
			{
				try
				{
					var originalFile = _originalFiles[index];
					var snapshotPath = Path.Combine(_snapshotBasePath, String.Format("{0:X8}", index));

					FileSystem.DeleteFile(originalFile.Path.AsPath());
					
					var fi = new FileInfo(snapshotPath);
					fi.MoveTo(originalFile.Path);					
					fi.LastWriteTimeUtc = originalFile.LastModified;					
					fi.Attributes = originalFile.Attributes;
				}
				catch (Exception e)
				{
					Trace.WarnFormat("Snapshot.Revert: {0}", e.Message);
				}
			}

			Cleanup();
		}

		public void Dispose()
		{
			Cleanup();			
		}

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
				Trace.WarnFormat("Snapshot.Cleanup: {0}", e.Message);
			}
		}
	}
}