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

		public Hyperlink(Uri href, string label)
		{
			if (href == null)
				throw new ArgumentException("Hyperlink(href, label): href must not be null");
			if (String.IsNullOrEmpty(label))
				throw new ArgumentException("Hyperlink(href, label): label must not be null or empty");

			Href = href;
			Label = label;
		}
		
		public Uri Href { get; private set; }

		[DataMember(Name = "Href")]
		// ReSharper disable once InconsistentNaming
		private string __Href
		{
			get { return Href.ToString(); }
			set { Href = new Uri(value); }
		}

		[DataMember]
		public string Label { get; private set; }

		public override string ToString()
		{
			var sb = new StringBuilder(256);
			sb.Append('[');

			if (!String.IsNullOrEmpty(Label))
				sb.Append(Label).Append('|');

			sb.Append(Href).Append(']');

			return sb.ToString();
		}		
	}
}