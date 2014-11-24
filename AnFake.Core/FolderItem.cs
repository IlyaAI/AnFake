using System;
using System.IO;
using System.Linq;

namespace AnFake.Core
{
	public sealed class FolderItem : IComparable<FolderItem>
	{
		private readonly FileSystemPath _path;		

		internal FolderItem(FileSystemPath path)
		{			
			_path = path;			
		}
		
		public FileSystemPath Path
		{
			get { return _path; }
		}		

		public string Name
		{
			get { return _path.LastName; }
		}

		public FileSystemPath Parent
		{
			get { return _path.Parent; }
		}

		public bool Exists()
		{
			return Directory.Exists(_path.Full);
		}

		public bool IsEmpty()
		{
			var fullPath = _path.Full;
			return 
				!Directory.Exists(fullPath) || 
				!Directory.EnumerateFileSystemEntries(fullPath).Any();
		}

		public override string ToString()
		{
			return _path.ToString();
		}

		private bool Equals(FolderItem other)
		{
			return _path.Equals(other._path);
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			if (ReferenceEquals(this, obj)) return true;
			return obj is FolderItem && Equals((FolderItem) obj);
		}

		public override int GetHashCode()
		{
			return _path.GetHashCode();
		}

		public int CompareTo(FolderItem other)
		{
			return _path.CompareTo(other._path);
		}

		public static implicit operator FileSystemPath(FolderItem folder)
		{
			return folder._path;
		}
	}
}