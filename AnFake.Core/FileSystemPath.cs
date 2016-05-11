using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using AnFake.Core.Exceptions;

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
	///     <para>
	///         You can use the following syntax for well-known folder substituation: [ProgramFilesX86], [Temp], etc.
	///         In square brackets is is possible to use any value from <c>Environment.SpecialFolder</c> and some additional
	///         values:
	///         <list type="bullet">
	///             <item>
	///                 <term>AnFake</term> <description>AnFake home directory</description>
	///             </item>
	///             <item>
	///                 <term>AnFakeExtras</term> <description>AnFake extras directory (usually [AnFake]/Extras)</description>
	///             </item>
	///             <item>
	///                 <term>Temp</term> <description>system temporary directory</description>
	///             </item>
	///             <item>
	///                 <term>PATH</term> <description>first path from PATH environment variable where file denoted by rest part of FileSystemPath exists</description>
	///             </item>	
	///         </list>
	///     </para>
	/// </remarks>
	/// <example>
	///     C:\Projects\MySolution\build.csx
	///     <code>
	///  var solution = "MySolution.sln".AsPath();            // refers to C:\Projects\MySolution\MySolution.sln	
	///  var projA = "ProjectA/ProjectA.csproj".AsPath();     // refers to C:\Projects\MySolution\ProjectA\ProjectA.csproj
	///  var projB = "ProjectB".AsPath() / "ProjectB.csproj"; // refers to C:\Projects\MySolution\ProjectB\ProjectB.csproj
	///  </code>
	///     C:\Projects\MySolution\build.fsx
	///     <code>
	///  let solution = ~~"MySolution.sln"            // refers to C:\Projects\MySolution\MySolution.sln	
	///  let projA = ~~"ProjectA/ProjectA.csproj"     // refers to C:\Projects\MySolution\ProjectA\ProjectA.csproj
	///  let projB = ~~"ProjectB" / "ProjectB.csproj" // refers to C:\Projects\MySolution\ProjectB\ProjectB.csproj
	///  </code>
	/// </example>
	public sealed class FileSystemPath : IComparable<FileSystemPath>
	{
		private static readonly char[] Wildcards = {'*', '?'};
		private static readonly string[] Steps = {".", ".."};

		private static FileSystemPath _basePath = Directory.GetCurrentDirectory().AsPath();

		/// <summary>
		///     Base path for relative one. Refers to folder where build script is located.
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
		///     Is path wildcarded (i.e. contains * or ? symbols)?
		/// </summary>
		public bool IsWildcarded
		{
			get { return _value.IndexOfAny(Wildcards) > 0; }
		}

		/// <summary>
		///     Is path rooted (i.e. started from / or \ symbols or drive letter)?
		/// </summary>
		public bool IsRooted
		{
			get { return Path.IsPathRooted(_value); }
		}

		/// <summary>
		///     Is this UNC path (i.e. started from // or \\ symbols)?
		/// </summary>
		public bool IsUnc
		{
			get { return _value.Length > 1 && _value[0] == Path.DirectorySeparatorChar && _value[1] == Path.DirectorySeparatorChar; }
		}

		/// <summary>
		///     String representation of path as was specified.
		/// </summary>
		/// <example>
		///     C:\Projects\MySolution\build.fsx
		///     <code>
		///  let relative = ~~"build.fsx"
		///  let spec = relative.Spec     // @"build.fsx"
		///  </code>
		///     <code>
		///  let absolute = ~~"C:/Projects/MySolution/build.fsx"
		///  let spec = absolute.Spec     // @"C:\Projects\MySolution\build.fsx"
		///  </code>
		/// </example>
		public string Spec
		{
			get { return _value; }
		}

		/// <summary>
		///     String representation of full path.
		/// </summary>
		/// <remarks>
		///     If path was created as relative then it automatically evaluated to full against <c>FileSystemPath.Base</c>.
		/// </remarks>
		/// <example>
		///     C:\Projects\MySolution\build.fsx
		///     <code>
		///  let relative = ~~"build.fsx"
		///  let full = relative.Full     // @"C:\Projects\MySolution\build.fsx"
		///  </code>
		///     <code>
		///  let absolute = ~~"C:/Projects/MySolution/build.fsx"
		///  let full = absolute.Full     // @"C:\Projects\MySolution\build.fsx"
		///  </code>
		///     <code>
		///  let absolute = ~~"C:/Projects/MyAnotherSolution/build.fsx"
		///  let full = absolute.Full     // @"C:\Projects\MyAnotherSolution\build.fsx"
		///  </code>
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
		///     Last name in the path steps including extension if any.
		/// </summary>
		/// <example>
		///     C:\Projects\MySolution\build.fsx
		///     <code>
		///  let path = ~~"build.fsx"
		///  let lastName = path.LastName  // "build.fsx"
		///  </code>
		///     <code>
		///  let path = ~~"C:/Projects/MySolution/build.fsx"
		///  let lastName = path.LastName  // "build.fsx"
		///  </code>
		///     <code>
		///  let path = ~~""
		///  let lastName = path.LastName  // "MySolution"
		///  </code>
		/// </example>
		/// <seealso cref="Path.GetFileName" />
		public string LastName
		{
			get { return Path.GetFileName(_value); }
		}

		/// <summary>
		///     Last name in the path steps without extension.
		/// </summary>
		/// <example>
		///     C:\Projects\MySolution\build.fsx
		///     <code>
		///  let path = ~~"build.fsx"
		///  let lastNameWoExt = path.LastNameWithoutExt  // "build"
		///  </code>
		///     <code>
		///  let path = ~~""
		///  let lastNameWoExt = path.LastNameWithoutExt  // "MySolution"
		///  </code>
		/// </example>
		/// <seealso cref="Path.GetFileNameWithoutExtension" />
		public string LastNameWithoutExt
		{
			get { return Path.GetFileNameWithoutExtension(_value); }
		}

		/// <summary>
		///     Extension with preceeded dot. <c>String.Empty</c> if none.
		/// </summary>
		/// <example>
		///     C:\Projects\MySolution\build.fsx
		///     <code>
		///  let path = ~~"build.fsx"
		///  let ext = path.Ext  // ".fsx"
		///  </code>
		///     <code>
		///  let path = ~~""
		///  let ext = path.Ext  // ""
		///  </code>
		/// </example>
		/// <seealso cref="Path.GetExtension" />
		public string Ext
		{
			get { return Path.GetExtension(_value); }
		}

		/// <summary>
		///     Does path have parent?
		/// </summary>
		/// <example>
		///     C:\Projects\MySolution\build.fsx
		///     <code>
		///  let path = ~~"MyProject/MyProject.csproj"
		///  let hasParent = path.HasParent  // true
		///  </code>
		///     <code>
		///  let path = ~~"solution.sln"
		///  let hasParent = path.HasParent  // false
		///  </code>
		///     <code>
		///  let path = ~~"C:/MySolution/build.fsx"
		///  let hasPparent1 = path.HasParent               // true
		///  let hasParent2 = path.Parent.HasParent         // true
		///  let hasParent3 = path.Parent.Parent.HasParent  // false
		///  </code>
		/// </example>
		public bool HasParent
		{
			get
			{
				var parent = _value.Length > 0 ? Path.GetDirectoryName(_value) : String.Empty;
				return !(parent == null || parent.Length == 0 && _value.Length == 0);
			}
		}

		/// <summary>
		///     Parent folder. If path is root or just file name then exception is thrown.
		/// </summary>
		/// <example>
		///     C:\Projects\MySolution\build.fsx
		///     <code>
		///  let path = ~~"MyProject/MyProject.csproj"
		///  let parent = path.Parent  // "MyProject"
		///  </code>
		///     <code>
		///  let path = ~~"solution.sln"
		///  let parent1 = path.Parent     // ""
		///  let parent2 = parent1.Parent  // throws InvalidConfigurationException
		///  </code>
		///     <code>
		///  let path = ~~"C:/MySolution/build.fsx"
		///  let parent1 = path.Parent     // @"C:\MySolution"
		///  let parent2 = parent1.Parent  // @"C:\"
		///  let parent3 = parent2.Parent  // throws InvalidConfigurationException
		///  </code>
		/// </example>
		/// <exception cref="InvalidConfigurationException">if path is root or just file name</exception>
		public FileSystemPath Parent
		{
			get
			{
				var parent = _value.Length > 0 ? Path.GetDirectoryName(_value) : String.Empty;
				if (parent == null || parent.Length == 0 && _value.Length == 0)
					throw new InvalidConfigurationException(String.Format("Path '{0}' does not have parent.", _value));

				return new FileSystemPath(parent, true);
			}
		}

		/// <summary>
		///     Converts this path to relative against given base.
		///     Throws an exception if this path isn't a sub-path of base one.
		/// </summary>
		/// <param name="basePath"></param>
		/// <returns>relative path</returns>
		public FileSystemPath ToRelative(FileSystemPath basePath)
		{
			if (basePath == null)
				throw new ArgumentException("FileSystemPath.ToRelative(basePath): basePath must not be null");

			var myFull = Path.GetFullPath(Full);
			var baseFull = Path.GetFullPath(basePath.Full);

			if (myFull.StartsWith(baseFull, StringComparison.OrdinalIgnoreCase))
				return new FileSystemPath(myFull.Substring(baseFull.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar), true);

			throw new InvalidConfigurationException(String.Format("Unable to evaluate relative path.\n  SourcePath: '{0}'\n  BasePath: '{1}'", myFull, baseFull));
		}

		/// <summary>
		///     Converts this path to UNC path.
		/// </summary>
		/// <returns>UNC path</returns>
		public FileSystemPath ToUnc()
		{
			var full = Path.GetFullPath(Full);

			if (full.Length < 2)
				throw new InvalidOperationException(String.Format("Path too short to be converted to UNC: '{0}'", full));

			if (full[0] == Path.DirectorySeparatorChar && full[1] == Path.DirectorySeparatorChar)
				return new FileSystemPath(full, true);

			if (full[1] != ':' || !(full[0] >= 'A' && full[0] <= 'Z' || full[0] >= 'a' && full[0] <= 'z'))
				throw new InvalidOperationException(String.Format("Don't know how to convert to UNC: '{0}'", full));

			var unc = new StringBuilder(full.Length + 64);
			unc.Append(Path.DirectorySeparatorChar).Append(Path.DirectorySeparatorChar)
				.Append(Environment.MachineName).Append(Path.DirectorySeparatorChar)
				.Append(full[0]).Append('$')
				.Append(full.Substring(2));

			return new FileSystemPath(unc.ToString(), true);
		}

		/// <summary>
		///     Converts this path to Unix-style path.
		/// </summary>
		/// <returns>Unix-style path</returns>
		public string ToUnix()
		{
			if (_value.Length >= 2 && _value[1] == ':')
				throw new InvalidOperationException(String.Format("Unix-style path cann't contain driver letter: '{0}'", _value));

			return _value.Replace(Path.DirectorySeparatorChar, '/');
		}

		/// <summary>
		///     Converts UNC path to URI.
		/// </summary>
		/// <remarks>
		///     Method throws an exception if this path is not UNC path.
		/// </remarks>
		/// <returns>file:// URI</returns>
		public Uri ToUri()
		{
			if (!IsUnc)
				throw new InvalidConfigurationException(String.Format("Only UNC path can be converted to URI: '{0}'", _value));

			return new Uri(_value);
		}

		/// <summary>
		///     Splits path onto steps.
		/// </summary>
		/// <returns>array of path steps</returns>
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
			var subSteps = subPath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

			var i = 0;
			var realBase = basePath;
			while (i < subSteps.Length && subSteps[i] == "..")
			{
				if (!realBase.HasParent)
					throw new InvalidConfigurationException(String.Format("Unable to combine paths: sub-path '{0}' is beyond of base '{1}'. ", subPath, basePath));

				realBase = realBase.Parent;
				i++;
			}

			return realBase/subSteps.AsPath(i, subSteps.Length - i);
		}

		public static FileSystemPath operator /(FileSystemPath basePath, FileSystemPath subPath)
		{
			return new FileSystemPath(Path.Combine(basePath._value, subPath._value), true);
		}

		public static FileSystemPath operator /(FileSystemPath basePath, Version subPath)
		{
			return new FileSystemPath(Path.Combine(basePath._value, subPath.ToString()), true);
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
			if (path == ".")
				return String.Empty;

			var nzPath = new StringBuilder(path.Length);

			var valid = true;
			var begin = 0;
			for (var index = 0; index <= path.Length; index++)
			{
				if (index == path.Length
					|| path[index] == Path.DirectorySeparatorChar
					|| path[index] == Path.AltDirectorySeparatorChar)
				{
					var len = index - begin;

					if (len == 1 && path[begin] == '.')
					{
						valid = false;
						break;
					}

					if (len == 2 && path[begin] == '.' && path[begin + 1] == '.')
					{
						valid = false;
						break;
					}

					nzPath.Append(path, begin, len);

					if (index < path.Length)
					{
						nzPath.Append(Path.DirectorySeparatorChar);
					}

					begin = index + 1;
				}
			}

			if (!valid)
				throw new InvalidConfigurationException(String.Format("Path '{0}' contains not allowed steps '.' and/or '..'", path));

			return nzPath.ToString();
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

			switch (macro)
			{
				case "Temp":
					return Path.Combine(Path.GetTempPath(), subPath);
				case "MonoLib":
					return Path.Combine(Mono.Lib, subPath);
				case "MonoBin":
					return Path.Combine(Mono.Bin, subPath);
				case "AnFake":
					return Path.Combine(
						// ReSharper disable once AssignNullToNotNullAttribute
						Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
						subPath);
				case "AnFakeExtras":
					return Path.Combine(
						// ReSharper disable once AssignNullToNotNullAttribute
						Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
						Path.Combine("Extras", subPath));
				case "PATH":
					return ResolvePATH(subPath);
			}

			Environment.SpecialFolder specialFolder;
			if (!Enum.TryParse(macro, false, out specialFolder))
				return path;

			return Path.Combine(Environment.GetFolderPath(specialFolder), subPath);
		}

		[SuppressMessage("ReSharper", "InconsistentNaming")]
		private static string ResolvePATH(string subPath)
		{
			var path = Environment.GetEnvironmentVariable("PATH");
			if (path == null)
				return subPath;

			var resolvedPath = path
				.Split(';')
				.Select(x => Path.Combine(x.Trim(), subPath))
				.FirstOrDefault(File.Exists);

			return resolvedPath ?? subPath;
		}
	}
}