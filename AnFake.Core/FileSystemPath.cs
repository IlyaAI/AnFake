using System;
using System.IO;

namespace AnFake.Core
{
	public sealed class FileSystemPath : IComparable<FileSystemPath>
	{
		private static FileSystemPath _basePath = new FileSystemPath(Directory.GetCurrentDirectory(), true);

		public static FileSystemPath Base
		{
			get { return _basePath; }
			internal set { _basePath = _basePath/value; }
		}

		private readonly string _value;

		internal FileSystemPath(string value, bool normalized)
		{
			_value = ExpandWellknownFolders(
				normalized 
					? value 
					: Normalize(value));
		}

		public bool IsWildcarded
		{
			get { return _value.IndexOfAny(new[] {'*', '?'}) > 0; }
		}

		public string Spec
		{
			get { return _value; }
		}

		public string Full
		{
			get
			{
				return ReferenceEquals(this, Base) 
					? Base._value 
					: Path.Combine(Base._value, _value);
			}
		}

		public string LastName
		{
			get { return Path.GetFileName(_value); }
		}

		public string LastNameWithoutExt
		{
			get { return Path.GetFileNameWithoutExtension(_value); }
		}

		public string Ext
		{
			get { return Path.GetExtension(_value); }
		}

		public FileSystemPath Parent
		{
			get { return new FileSystemPath(Path.GetDirectoryName(_value), true); }
		}

		public FileSystemPath ToRelative(FileSystemPath basePath)
		{
			// TODO check basePath for null

			var myFull = Full;
			var baseFull = basePath.Full;

			return myFull.StartsWith(baseFull, StringComparison.InvariantCultureIgnoreCase)
				? new FileSystemPath(myFull.Substring(baseFull.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar), true)
				: this;
		}

		public string[] Split()
		{
			return _value.Split(Path.DirectorySeparatorChar);
		}

		public override int GetHashCode()
		{
			return StringComparer.InvariantCultureIgnoreCase.GetHashCode(_value);
		}

		public int CompareTo(FileSystemPath other)
		{
			return StringComparer.InvariantCultureIgnoreCase.Compare(_value, other._value);
		}

		private bool Equals(FileSystemPath other)
		{
			return StringComparer.InvariantCultureIgnoreCase.Equals(_value, other._value);
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			if (ReferenceEquals(this, obj)) return true;
			return obj is FileSystemPath && Equals((FileSystemPath) obj);
		}		

		public override string ToString()
		{
			return _value;
		}

		public static bool operator ==(FileSystemPath left, FileSystemPath right)
		{
			return Equals(left, right);
		}

		public static bool operator !=(FileSystemPath left, FileSystemPath right)
		{
			return !Equals(left, right);
		}

		public static FileSystemPath operator /(FileSystemPath basePath, string subPath)
		{
			return new FileSystemPath(Path.Combine(basePath._value, Normalize(subPath)), true);
		}

		public static FileSystemPath operator /(FileSystemPath basePath, FileSystemPath subPath)
		{
			return new FileSystemPath(Path.Combine(basePath._value, subPath._value), true);
		}

		public static FileSet operator %(FileSystemPath basePath, FileSystemPath wildcardedPath)
		{
			return new FileSet().From(basePath).Include(wildcardedPath);
		}

		public static FileSet operator %(FileSystemPath basePath, string wildcardedPath)
		{
			return new FileSet().From(basePath).Include(wildcardedPath);
		}

		private static string Normalize(string path)
		{
			return path.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
		}

		private static string ExpandWellknownFolders(string path)
		{
			if (!path.StartsWith("[", StringComparison.InvariantCulture))
				return path;

			var end = path.IndexOf("]", StringComparison.InvariantCulture);
			if (end < 0)
				return path;

			var macro = path.Substring(1, end - 1);

			Environment.SpecialFolder specialFolder;
			if (!Enum.TryParse(macro, true, out specialFolder))
				return path;

			return Environment.GetFolderPath(specialFolder) + path.Substring(end + 1);
		}
	}
}