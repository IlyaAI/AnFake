using System;

namespace AnFake.Core
{
	/// <summary>
	///     Represents a file item inside archive (zip, gzip, tar).
	/// </summary>	
	public sealed class ZippedFileItem
	{
		private readonly string _path;		
		
		internal ZippedFileItem(string path)
		{
			_path = path;			
		}

		public string Path
		{
			get { return _path; }
		}
		
		public string Name
		{
			get { return System.IO.Path.GetFileName(_path); }
		}

		public string Parent
		{
			get { return System.IO.Path.GetDirectoryName(_path); }
		}

		public string NameWithoutExt
		{
			get { return System.IO.Path.GetFileNameWithoutExtension(_path); }
		}

		public string Ext
		{
			get { return System.IO.Path.GetExtension(_path); }
		}

		public override string ToString()
		{
			return _path;
		}

		private bool Equals(ZippedFileItem other)
		{
			return String.Equals(_path, other._path);
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			if (ReferenceEquals(this, obj)) return true;
			return obj is ZippedFileItem && Equals((ZippedFileItem) obj);
		}

		public override int GetHashCode()
		{
			return _path.GetHashCode();			
		}

		public static bool operator ==(ZippedFileItem left, ZippedFileItem right)
		{
			return Equals(left, right);
		}

		public static bool operator !=(ZippedFileItem left, ZippedFileItem right)
		{
			return !Equals(left, right);
		}
	}
}