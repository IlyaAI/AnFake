using System;
using System.Runtime.Serialization;
using System.Text;

namespace AnFake.Api
{
	[DataContract]
	public sealed class Hyperlink
	{
		private Hyperlink()
		{
		}

		public Hyperlink(string href, string label)
		{
			if (href == null)
				throw new ArgumentException("Hyperlink(href, label): href must not be null");
			if (String.IsNullOrEmpty(label))
				throw new ArgumentException("Hyperlink(href, label): label must not be null or empty");

			Href = href;
			Label = label;
		}

		[DataMember]
		public string Href { get; private set; }

		[DataMember]
		public string Label { get; private set; }

		public bool IsLocal()
		{
			return IsLocal(Href);
		}

		public override string ToString()
		{
			if (String.IsNullOrEmpty(Href))
				return String.Empty;

			var sb = new StringBuilder(256);
			sb.Append('[');

			if (!String.IsNullOrEmpty(Label))
				sb.Append(Label).Append('|');

			sb.Append(Href).Append(']');

			return sb.ToString();
		}

		public static bool IsLocal(string href)
		{
			if (href == null)
				throw new ArgumentException("Hyperlink.IsLocal(href): href must not be null");

			if (href.StartsWith(@"\\")) // this is UNC
				return false;

			var index = href.IndexOf(':');
			if (index < 0) // there is no schema prefix so treat as local
				return true;

			var schema = href.Substring(0, index);
			if (schema.Length == 1 
				&& (schema[0] >= 'A' && schema[0] <= 'Z' 
					|| (schema[0] >= 'a' && schema[0] <= 'z'))) // this is drive letter so treat as local
				return true;

			return schema.Equals("file", StringComparison.OrdinalIgnoreCase);
		}
	}
}