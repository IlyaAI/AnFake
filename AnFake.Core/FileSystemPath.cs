using System;
using System.IO;

namespace AnFake.Core
{
	public sealed class FileSystemPath : IComparable<FileSystemPath>
	{
		public static FileSystemPath Base { get; internal set; }

		static FileSystemPath()
		{
			Base = new FileSystemPath(Directory.GetCurrentDirectory(), true);			
		}

		private readonly string _value;

		internal FileSystemPath(string value, bool normalized)
		{
			_value = normalized 
				? value 
				: Normalize(value);
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

		/*public static implicit operator String(FilePath path)
		{
			return path._value;
		}*/

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

		private static string Normalize(string path)
		{
			return path.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
		}
	}
}