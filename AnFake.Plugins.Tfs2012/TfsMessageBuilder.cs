using System;
using System.Collections.Generic;
using System.Text;
using AnFake.Api;

namespace AnFake.Plugins.Tfs2012
{
	internal sealed class TfsMessageBuilder
	{
		private readonly StringBuilder _message = new StringBuilder(512);		

		public TfsMessageBuilder Append(string text)
		{
			_message.Append(text);

			return this;
		}

		public TfsMessageBuilder AppendFormat(string format, params object[] args)
		{
			_message.AppendFormat(format, args);

			return this;
		}

		public TfsMessageBuilder AppendLink(Hyperlink link)
		{
			return AppendLink(link.Label, link.Href);
		}

		public TfsMessageBuilder AppendLink(Uri href)
		{
			return AppendLink(href.ToString(), href);
		}

		public TfsMessageBuilder AppendLink(string label, Uri href)
		{
			_message.Append('[').Append(label).Append("](").Append(href).Append(')');
			
			return this;
		}

		public TfsMessageBuilder AppendLinks(IList<Hyperlink> links, string prefix = " ", string separator = " | ", string suffix = "")
		{
			if (links.Count == 0)
				return this;

			var startedAt = _message.Length;
			_message.Append(prefix);

			var prevLength = _message.Length;
			AppendLink(links[0]);

			for (var i = 1; i < links.Count; i++)
			{
				if (_message.Length > prevLength)
				{
					_message.Append(separator);
				}

				prevLength = _message.Length;
				AppendLink(links[i]);
			}

			// if no one link generated then remove prefix
			if (_message.Length - startedAt == prefix.Length)
			{
				_message.Remove(startedAt, _message.Length - startedAt);
			}
			else
			{
				_message.Append(suffix);
			}

			return this;
		}

		public TfsMessageBuilder NewLine()
		{
			_message.AppendLine();

			return this;
		}

		public void Clear()
		{
			_message.Clear();
		}

		public override string ToString()
		{
			return _message.ToString();
		}		
	}
}