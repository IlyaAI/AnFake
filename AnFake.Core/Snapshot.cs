using System;
using System.Collections.Generic;
using System.IO;
using AnFake.Api;
using AnFake.Core.Exceptions;

namespace AnFake.Core
{
	/// <summary>
	///		Represents per-file basis snapshot.
	/// </summary>
	/// <remarks>
	///		This helper class might be used in cases when some files modified temporary and should be reverted after build.
	/// </remarks>
	public sealed class Snapshot : IDisposable
	{
		/// <summary>
		///		Represents saved file.
		/// </summary>
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
		private bool _initialized;

		/// <summary>
		///		Constructs new shanshot instance.
		/// </summary>
		public Snapshot()
		{
			_snapshotBasePath = Path.Combine(Path.GetTempPath(), "AnFake.Snapshot".MakeUnique());
		}

		/// <summary>
		///		Saves given files.
		/// </summary>
		/// <param name="files">files to be saved (not null)</param>
		public void Save(IEnumerable<FileItem> files)
		{
			if (files == null)
				throw new ArgumentException("Snapshot.Save(files): files must not be null");

			foreach (var file in files)
			{
				Save(file);
			}
		}

		/// <summary>
		///		Saves given file.
		/// </summary>
		/// <param name="file">file to be saved (not null)</param>
		public void Save(FileItem file)
		{
			if (file == null)
				throw new ArgumentException("Snapshot.Save(file): file must not be null");

			if (!_initialized)
			{
				Directory.CreateDirectory(_snapshotBasePath);
				_initialized = true;
			}

			var fullPath = file.Path.Full;
			var snapshotPath = Path.Combine(_snapshotBasePath, String.Format("{0:X8}", _originalFiles.Count));
			var fi = new FileInfo(fullPath);
			var lastModified = fi.LastWriteTimeUtc;			
			var attributes = fi.Attributes;

			Trace.DebugFormat("Snapshot.Save: {0} => {1}", fullPath, snapshotPath);
			fi.CopyTo(snapshotPath);

			var fs = new SavedFile(file.Path, lastModified, attributes);

			_originalFiles.Add(fs);

			if (FileSaved != null)
			{
				FileSaved.Invoke(this, fs);
			}
		}

		/// <summary>
		///		Reverts all files saved to this snapshot.
		/// </summary>
		/// <remarks>
		///		Method never throws an exception and does the best effort to restore each file.
		/// </remarks>
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

		/// <summary>
		///		Clean-ups all temporary files.
		/// </summary>
		public void Dispose()
		{
			Cleanup();			
		}

		/// <summary>
		///		Fired when file is saved.
		/// </summary>
		public static event EventHandler<SavedFile> FileSaved;

		/// <summary>
		///		Fired when file is reverted.
		/// </summary>
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