using System;
using System.IO;

namespace AnFake.Core
{
	public sealed class FileItem : IComparable<FileItem>
	{
		private readonly FileSystemPath _path;
		private readonly FileSystemPath _basePath;
		private FileInfo _info;

		internal FileItem(FileSystemPath path, FileSystemPath basePath)
		{			
			_path = path;
			_basePath = basePath;
		}		

		public FileSystemPath Path
		{
			get { return _path; }
		}

		public FileSystemPath BasePath
		{
			get { return _basePath; }
		}

		public FileSystemPath RelPath
		{
			get
			{
				// TODO: what if basePath is null?
				return _path.ToRelative(_basePath);
			}
		}

		public string Name
		{
			get { return _path.LastName; }
		}

		public FileSystemPath Folder
		{
			get { return _path.Parent; }
		}

		public long Length
		{
			get { return Info.Length; }
		}

		internal FileInfo Info
		{
			get { return _info ?? (_info = new FileInfo(_path.Full)); }
		}

		public string NameWithoutExt
		{
			get { return _path.LastNameWithoutExt; }
		}

		public string Ext
		{
			get { return _path.Ext; }
		}

		public bool Exists()
		{
			return File.Exists(_path.Full);
		}

		public void Append(string format, params object[] args)
		{
			File.AppendAllText(_path.Full, String.Format(format, args));
		}

		public override string ToString()
		{
			return _path.ToString();
		}

		private bool Equals(FileItem other)
		{
			return _path.Equals(other._path);
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			if (ReferenceEquals(this, obj)) return true;
			return obj is FileItem && Equals((FileItem) obj);
		}

		public override int GetHashCode()
		{
			return _path.GetHashCode();
		}

		public int CompareTo(FileItem other)
		{
			return _path.CompareTo(other._path);
		}

		public static implicit operator FileSystemPath(FileItem file)
		{
			return file._path;
		}
	}
}