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
	}
}