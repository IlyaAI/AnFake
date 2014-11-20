using System;
using System.Collections.Generic;
using System.IO;
using Common.Logging;

namespace AnFake.Core
{
	public sealed class Snapshot : IDisposable
	{
		private static readonly ILog Log = LogManager.GetLogger<Snapshot>();

		private readonly IList<string> _originalPathes = new List<string>();
		private readonly string _snapshotBasePath;		

		public Snapshot()
		{
			_snapshotBasePath = Path.Combine(Path.GetTempPath(), "".MakeUnique());
			Directory.CreateDirectory(_snapshotBasePath);
		}

		public void Save(FileSystemPath filePath)
		{			
			var fullPath = filePath.Full;
			var snapshotPath = Path.Combine(_snapshotBasePath, String.Format("{0:X8}", _originalPathes.Count));

			Log.DebugFormat("Save: {0} => {1}", fullPath, snapshotPath);
			File.Copy(fullPath, snapshotPath);

			_originalPathes.Add(fullPath);
		}

		public void Revert()
		{
			for (var index = 0; index < _originalPathes.Count; index++)
			{
				try
				{
					var originalPath = _originalPathes[index];
					var snapshotPath = Path.Combine(_snapshotBasePath, String.Format("{0:X8}", index));

					FileSystem.DeleteFile(originalPath.AsPath());
					File.Move(snapshotPath, originalPath);
				}
				catch (Exception e)
				{
					Log.Error("Revert FAILED.", e);
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
			_originalPathes.Clear();

			if (!Directory.Exists(_snapshotBasePath))
				return;

			try
			{
				Directory.Delete(_snapshotBasePath, true);
			}
			catch (Exception e)
			{
				Log.Error("Cleanup FAILED.", e);
			}
		}
	}
}