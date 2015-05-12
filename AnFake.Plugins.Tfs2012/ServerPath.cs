using System;
using System.IO;
using AnFake.Core.Exceptions;
using AnFake.Integration.Tfs2012;

namespace AnFake.Plugins.Tfs2012
{
	/// <summary>
	///		Represents path in TFS source control repo.
	/// </summary>
	public sealed class ServerPath : IComparable<ServerPath>
	{
		private readonly string _value;

		internal ServerPath(string value, bool normalized)
		{
			if (value == null)
				throw new ArgumentNullException("value");

			_value = normalized ? value : ServerPathUtils.Normalize(value);
		}

		/// <summary>
		///		Is path rooted (i.e. started from $/)?
		/// </summary>
		public bool IsRooted
		{
			get { return ServerPathUtils.IsRooted(_value); }
		}

		/// <summary>
		///		String representation of path as was specified when constructed.
		/// </summary>
		public string Spec
		{
			get { return _value; }
		}

		/// <summary>
		///		String representation of full path.
		/// </summary>
		/// <remarks>
		///		Relative paths aren't support this method because there is no well defined base for relative server paths.
		/// </remarks>
		public string Full
		{
			get
			{
				if (!IsRooted)
					throw new InvalidConfigurationException(String.Format("ServerPath.Full unavailable because path is relative: {0}", _value));

				return _value;
			}
		}

		/// <summary>
		///		Last name in the path steps including extension if any.
		/// </summary>
		public string LastName
		{
			get { return Path.GetFileName(_value); }
		}

		/// <summary>
		///		Last name in the path steps without extension.
		/// </summary>
		public string LastNameWithoutExt
		{
			get { return Path.GetFileNameWithoutExtension(_value); }
		}

		/// <summary>
		///		Extension with preceeded dot. <c>String.Empty</c> if none.
		/// </summary>
		public string Ext
		{
			get { return Path.GetExtension(_value); }
		}

		/// <summary>
		///		Does path have parent?
		/// </summary>		
		public bool HasParent
		{
			get
			{
				var parent = Path.GetDirectoryName(_value);
				return !String.IsNullOrEmpty(parent);
			}
		}

		/// <summary>
		///		Parent folder. If path is root or just file name then exception is thrown.
		/// </summary>
		public ServerPath Parent
		{
			get
			{
				var parent = Path.GetDirectoryName(_value);
				if (String.IsNullOrEmpty(parent))
					throw new InvalidConfigurationException(String.Format("Server path '{0}' does not have parent.", _value));

				return new ServerPath(parent, true);
			}
		}

		/// <summary>
		///		Splits path onto steps.
		/// </summary>
		/// <returns>array of path steps</returns>
		public string[] Split()
		{
			return _value.Split(ServerPathUtils.SeparatorChar);
		}

		/// <summary>
		///		Converts this path to relative against given base.
		///		Returns this path if it isn't a sub-path of base one.
		/// </summary>
		/// <param name="basePath"></param>
		/// <returns>relative path</returns>
		public ServerPath ToRelative(ServerPath basePath)
		{
			if (basePath == null)
				throw new ArgumentException("ServerPath.ToRelative(basePath): basePath must not be null");

			var myFull = Full;
			var baseFull = basePath.Full;

			return myFull.StartsWith(baseFull, StringComparison.OrdinalIgnoreCase)
				? new ServerPath(myFull.Substring(baseFull.Length).TrimStart(ServerPathUtils.SeparatorChar, ServerPathUtils.AltSeparatorChar), true)
				: this;
		}

		public override int GetHashCode()
		{
			return StringComparer.OrdinalIgnoreCase.GetHashCode(_value);
		}

		public int CompareTo(ServerPath other)
		{
			return StringComparer.OrdinalIgnoreCase.Compare(_value, other._value);
		}

		private bool Equals(ServerPath other)
		{
			return StringComparer.OrdinalIgnoreCase.Equals(_value, other._value);
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			if (ReferenceEquals(this, obj)) return true;
			return obj is ServerPath && Equals((ServerPath) obj);
		}		

		public override string ToString()
		{
			return _value;
		}		

		/// <summary>
		///		Returns true if paths are equals and false otherwise.
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns></returns>
		public static bool operator ==(ServerPath left, ServerPath right)
		{
			return Equals(left, right);
		}

		/// <summary>
		///		Returns true if paths aren't equals and false otherwise.
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns></returns>
		public static bool operator !=(ServerPath left, ServerPath right)
		{
			return !Equals(left, right);
		}

		/// <summary>
		///		Combines base path and sub path.
		/// </summary>
		/// <param name="basePath"></param>
		/// <param name="subPath"></param>
		/// <returns>combined path</returns>
		public static ServerPath operator /(ServerPath basePath, string subPath)
		{
			return new ServerPath(
				ServerPathUtils.Combine(
					basePath._value, 
					ServerPathUtils.Normalize(subPath)),
				true);
		}

		/// <summary>
		///		Combines base path and sub path.
		/// </summary>
		/// <param name="basePath"></param>
		/// <param name="subPath"></param>
		/// <returns>combined path</returns>
		public static ServerPath operator /(ServerPath basePath, ServerPath subPath)
		{
			return new ServerPath(
				ServerPathUtils.Combine(
					basePath._value, 
					subPath._value), 
				true);
		}

		/// <summary>
		///		Combines path with file name.
		/// </summary>
		/// <param name="basePath"></param>
		/// <param name="fileName"></param>
		/// <returns>path with file name</returns>
		public static ServerPath operator +(ServerPath basePath, string fileName)
		{
			// TODO: check fileName doesn't contain path separators
			return new ServerPath(basePath._value + fileName, true);
		}		
	}
}