using System;
using System.IO;
using System.Reflection;
using AnFake.Core.Exceptions;

namespace AnFake.Core
{
	public sealed class FileSystemPath : IComparable<FileSystemPath>
	{
		private static FileSystemPath _basePath = Directory.GetCurrentDirectory().AsPath();

		public static FileSystemPath Base
		{
			get { return _basePath; }
			internal set
			{
				if (value == null)
					throw new AnFakeArgumentException("FileSystemPath.Base must not be null");

				if (!Path.IsPathRooted(value.Spec))
					throw new AnFakeArgumentException("FileSystemPath.Base must be absolute path");

				_basePath = value;
			}
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

		public bool IsRooted
		{
			get { return Path.IsPathRooted(_value); }
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

			return myFull.StartsWith(baseFull, StringComparison.OrdinalIgnoreCase)
				? new FileSystemPath(myFull.Substring(baseFull.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar), true)
				: this;
		}

		public string[] Split()
		{
			return _value.Split(Path.DirectorySeparatorChar);
		}

		public override int GetHashCode()
		{
			return StringComparer.OrdinalIgnoreCase.GetHashCode(_value);
		}

		public int CompareTo(FileSystemPath other)
		{
			return StringComparer.OrdinalIgnoreCase.Compare(_value, other._value);
		}

		private bool Equals(FileSystemPath other)
		{
			return StringComparer.OrdinalIgnoreCase.Equals(_value, other._value);
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			if (ReferenceEquals(this, obj)) return true;
			return obj is FileSystemPath && Equals((FileSystemPath) obj);
		}		

		public override string ToString()
		{
			return _value == String.Empty ? "." : _value;
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
			return basePath / new FileSystemPath(subPath, false);
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

		public static FileSystemPath operator +(FileSystemPath basePath, string fileName)
		{
			// TODO: check fileName doesn't contain path separators
			return new FileSystemPath(basePath._value + fileName, true);
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
			var subPath = path.Substring(end + 1).TrimStart(Path.DirectorySeparatorChar);

			if (macro == "Temp")
				return Path.Combine(Path.GetTempPath(), subPath);

			// ReSharper disable AssignNullToNotNullAttribute
			if (macro == "AnFake")
				return Path.Combine(
					Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), 
					subPath);

			if (macro == "AnFakePlugins")
				return Path.Combine(
					Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
					Path.Combine("Plugins", subPath));

			if (macro == "AnFakeExtras")
				return Path.Combine(
					Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
					Path.Combine("Extras", subPath));
			// ReSharper restore AssignNullToNotNullAttribute

			Environment.SpecialFolder specialFolder;
			if (!Enum.TryParse(macro, false, out specialFolder))
				return path;

			return Path.Combine(Environment.GetFolderPath(specialFolder), subPath);
		}
	}
}