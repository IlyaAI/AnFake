using System;
using System.IO;
using AnFake.Core.Exceptions;
using AnFake.Integration.Tfs2012;

namespace AnFake.Plugins.Tfs2012
{
	public sealed class ServerPath : IComparable<ServerPath>
	{
		private readonly string _value;

		internal ServerPath(string value, bool normalized)
		{
			if (value == null)
				throw new ArgumentNullException("value");

			_value = normalized ? value : ServerPathUtils.Normalize(value);
		}

		public bool IsRooted
		{
			get { return ServerPathUtils.IsRooted(_value); }
		}

		public string Spec
		{
			get { return _value; }
		}

		public string Full
		{
			get
			{
				if (!IsRooted)
					throw new InvalidConfigurationException(String.Format("ServerPath.Full unavailable because path is relative: {0}", _value));

				return _value;
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

		public ServerPath Parent
		{
			get { return new ServerPath(Path.GetDirectoryName(_value), true); }
		}		

		public string[] Split()
		{
			return _value.Split(ServerPathUtils.SeparatorChar);
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

		public static bool operator ==(ServerPath left, ServerPath right)
		{
			return Equals(left, right);
		}

		public static bool operator !=(ServerPath left, ServerPath right)
		{
			return !Equals(left, right);
		}

		public static ServerPath operator /(ServerPath basePath, string subPath)
		{
			return new ServerPath(
				ServerPathUtils.Combine(
					basePath._value, 
					ServerPathUtils.Normalize(subPath)),
				true);
		}

		public static ServerPath operator /(ServerPath basePath, ServerPath subPath)
		{
			return new ServerPath(
				ServerPathUtils.Combine(
					basePath._value, 
					subPath._value), 
				true);
		}

		public static ServerPath operator +(ServerPath basePath, string fileName)
		{
			// TODO: check fileName doesn't contain path separators
			return new ServerPath(basePath._value + fileName, true);
		}		
	}
}