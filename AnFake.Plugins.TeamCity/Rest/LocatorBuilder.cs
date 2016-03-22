using System;
using System.Net;
using System.Text;

namespace AnFake.Plugins.TeamCity.Rest
{
	internal sealed class LocatorBuilder
	{
		private readonly StringBuilder _locator = new StringBuilder();

		public LocatorBuilder Append(string name, string value)
		{
			if (_locator.Length > 0)
			{
				_locator.Append(',');
			}

			_locator.Append(name).Append(':');

			if (value.IndexOfAny(new[] {':', ','}) >= 0)
			{
				_locator.Append('(').Append(value).Append(')');
			}
			else
			{
				_locator.Append(value);
			}

			return this;
		}

		public string ToQuery()
		{
			return "?locator=" + WebUtility.UrlEncode(_locator.ToString());
		}

		public string ToPath()
		{
			return WebUtility.UrlEncode(_locator.ToString());
		}
	}
}