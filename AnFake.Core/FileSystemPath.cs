using System;
using System.IO;
using System.Reflection;

namespace AnFake.Core
{
	/// <summary>
	///     Represents path to file system item either file or folder.
	/// </summary>
	/// <remarks>
	///     <para>
	///         Path might be an absolute (rooted) or relative. Relative path is evaluated against folder where build script is
	///         located.
	///     </para>
	///     <para>
	///         FileSystemPath is instantiated from string by an extension method AsPath or operator ~~ in F#. Pathes might be
	///         combined via operator /.
	///     </para>
	///     <para>
	///         Both separators / and \ are supported. But / is preferred because it doesn't require slash doubling or string
	///         prefix @.
	///     </para>
	/// </remarks>
	/// <example>
	/// C:\Projects\MySolution\build.csx
	/// <code>
	/// var solution = "MySolution.sln".AsPath();            // refers to C:\Projects\MySolution\MySolution.sln	
	/// var projA = "ProjectA/ProjectA.csproj".AsPath();     // refers to C:\Projects\MySolution\ProjectA\ProjectA.csproj
	/// var projB = "ProjectB".AsPath() / "ProjectB.csproj"; // refers to C:\Projects\MySolution\ProjectB\ProjectB.csproj
	/// </code>
	/// C:\Projects\MySolution\build.fsx
	/// <code>
	/// let solution = ~~"MySolution.sln"            // refers to C:\Projects\MySolution\MySolution.sln	
	/// let projA = ~~"ProjectA/ProjectA.csproj"     // refers to C:\Projects\MySolution\ProjectA\ProjectA.csproj
	/// let projB = ~~"ProjectB" / "ProjectB.csproj" // refers to C:\Projects\MySolution\ProjectB\ProjectB.csproj
	/// </code>
	/// </example>
	public sealed class FileSystemPath : IComparable<FileSystemPath>
	{
		private static FileSystemPath _basePath = Directory.GetCurrentDirectory().AsPath();

		/// <summary>
		///		Base path for relative one. Refers to folder where build script is located.
		/// </summary>
		public static FileSystemPath Base
		{
			get { return _basePath; }
			internal set
			{
				if (value == null)
					throw new ArgumentException("FileSystemPath.Base must not be null");

				if (!Path.IsPathRooted(value.Spec))
					throw new ArgumentException("FileSystemPath.Base must be absolute path");

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

		/// <summary>
		///		Is path wildcarded (i.e. contains * or ? symbols)?
		/// </summary>
		public bool IsWildcarded
		{
			get { return _value.IndexOfAny(new[] {'*', '?'}) > 0; }
		}

		/// <summary>
		///		Is path rooted (i.e. started from / or \ symbols or drive letter)?
		/// </summary>
		public bool IsRooted
		{
			get { return Path.IsPathRooted(_value); }
		}

		/// <summary>
		///		String representation of path as was specified.
		/// </summary>
		/// <example>
		/// C:\Projects\MySolution\build.fsx
		///	<code>
		/// let relative = ~~"build.fsx"
		/// let spec = relative.Spec     // @"build.fsx"
		/// </code>
		/// <code>
		/// let absolute = ~~"C:/Projects/MySolution/build.fsx"
		/// let spec = absolute.Spec     // @"C:\Projects\MySolution\build.fsx"
		/// </code>
		/// </example>
		public string Spec
		{
			get { return _value; }
		}

		/// <summary>
		///		String representation of full path.
		/// </summary>
		/// <remarks>
		///		If path was created as relative then it automatically evaluated to full against <c>FileSystemPath.Base</c>.
		/// </remarks>
		/// <example>
		/// C:\Projects\MySolution\build.fsx
		///	<code>
		/// let relative = ~~"build.fsx"
		/// let full = relative.Full     // @"C:\Projects\MySolution\build.fsx"
		/// </code>
		/// <code>
		/// let absolute = ~~"C:/Projects/MySolution/build.fsx"
		/// let full = absolute.Full     // @"C:\Projects\MySolution\build.fsx"
		/// </code>
		/// <code>
		/// let absolute = ~~"C:/Projects/MyAnotherSolution/build.fsx"
		/// let full = absolute.Full     // @"C:\Projects\MyAnotherSolution\build.fsx"
		/// </code>
		/// </example>
		public string Full
		{
			get
			{
				return ReferenceEquals(this, Base)
					? Base._value
					: Path.Combine(Base._value, _value);
			}
		}

		/// <summary>
		///		Last name in the path steps including extension if any.
		/// </summary>
		/// <example>
		/// C:\Projects\MySolution\build.fsx
		///	<code>
		/// let path = ~~"build.fsx"
		/// let lastName = path.LastName  // "build.fsx"
		/// </code>
		/// <code>
		/// let path = ~~"C:/Projects/MySolution/build.fsx"
		/// let lastName = path.LastName  // "build.fsx"
		/// </code>
		/// <code>
		/// let path = ~~""
		/// let lastName = path.LastName  // "MySolution"
		/// </code>
		/// </example>
		/// <seealso cref="Path.GetFileName"/>
		public string LastName
		{
			get { return Path.GetFileName(_value); }
		}

		/// <summary>
		///		Last name in the path steps without extension.
		/// </summary>
		/// <example>
		/// C:\Projects\MySolution\build.fsx
		///	<code>
		/// let path = ~~"build.fsx"
		/// let lastNameWoExt = path.LastNameWithoutExt  // "build"
		/// </code>
		/// <code>
		/// let path = ~~""
		/// let lastNameWoExt = path.LastNameWithoutExt  // "MySolution"
		/// </code>
		/// </example>
		/// <seealso cref="Path.GetFileNameWithoutExtension"/>
		public string LastNameWithoutExt
		{
			get { return Path.GetFileNameWithoutExtension(_value); }
		}

		/// <summary>
		///		Extension with preceeded dot. <c>String.Empty</c> if none.
		/// </summary>
		/// <example>
		/// C:\Projects\MySolution\build.fsx
		///	<code>
		/// let path = ~~"build.fsx"
		/// let ext = path.Ext  // ".fsx"
		/// </code>
		/// <code>
		/// let path = ~~""
		/// let ext = path.Ext  // ""
		/// </code>
		/// </example>
		/// <seealso cref="Path.GetExtension"/>
		public string Ext
		{
			get { return Path.GetExtension(_value); }
		}

		/// <summary>
		///		
		/// </summary>
		public FileSystemPath Parent
		{
			get { return new FileSystemPath(Path.GetDirectoryName(_value), true); }
		}

		public FileSystemPath ToRelative(FileSystemPath basePath)
		{
			if (basePath == null)
				throw new ArgumentException("FileSystemPath.ToRelative(basePath): basePath must not be null");

			var myFull = Path.GetFullPath(Full);
			var baseFull = Path.GetFullPath(basePath.Full);

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
			return basePath/new FileSystemPath(subPath, false);
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