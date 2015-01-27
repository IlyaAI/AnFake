using System;
using System.Collections.Generic;
using System.Text;
using AnFake.Api;
using AnFake.Core;

namespace AnFake.Plugins.Tfs2012
{
	internal sealed class TfsMessageBuilder
	{
		private readonly StringBuilder _message = new StringBuilder(512);		
		private readonly FileSystemPath _dropPath;
		private bool _hasDropErrors;
		private bool _localLinksEnabled;

		internal TfsMessageBuilder(FileSystemPath dropPath)
		{
			_dropPath = dropPath;
		}

		public bool HasDropErrors
		{
			get { return _hasDropErrors; }
		}

		public TfsMessageBuilder EnableLocalLinks(bool enable = true)
		{
			_localLinksEnabled = enable;

			return this;
		}

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

		public TfsMessageBuilder AppendLink(string href)
		{
			return AppendLink(href, href);
		}

		public TfsMessageBuilder AppendLink(string label, string href)
		{
			if (Hyperlink.IsLocal(href))
			{
				href = PrepareLink(href.AsPath());
				if (href == null)
					return this;
			}

			_message.Append('[').Append(label).Append("](").Append(href).Append(')');
			
			return this;
		}

		public TfsMessageBuilder AppendLink(string label, Uri href)
		{
			return AppendLink(label, href.ToString());
		}

		public TfsMessageBuilder AppendLink(string label, FileSystemPath path)
		{
			var href = PrepareLink(path);

			return href != null
				? AppendLink(label, href)
				: this;
		}

		public TfsMessageBuilder AppendLink(FileSystemPath path)
		{
			var href = PrepareLink(path);

			return href != null
				? AppendLink(href, href)
				: this;
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

		private string PrepareLink(FileSystemPath path)
		{
			if (path.IsUnc)
				return path.Full;

			if (_localLinksEnabled)
				return path.ToUnc().Full;

			if (_dropPath == null)
				return null;

			var dstPath = _dropPath / path.LastName;

			if (path.AsFolder().Exists())
			{
				Log.ErrorFormat("TfsPlugin: links to local folders aren't supported. Href = '{0}'.", path);
				_hasDropErrors = true;
				return null;
			}

			if (SafeOp.Try(CopyUnique, path, dstPath))
			{
				return dstPath.Full;
			}
			
			_hasDropErrors = true;
			return null;			
		}

		private static void CopyUnique(FileSystemPath srcPath, FileSystemPath dstPath)
		{
			var dstFile = dstPath.AsFile();
			if (dstFile.Exists())
			{
				var folder = dstFile.Folder;
				var ext = dstFile.Ext;
				var uniqueName = NameGen.Generate(
					dstFile.NameWithoutExt, name => !(folder/name + ext).AsFile().Exists());

				Files.Copy(srcPath, folder / uniqueName);
			}
			else
			{
				Files.Copy(srcPath, dstPath);
			}
		}
	}
}