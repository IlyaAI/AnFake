using System;
using System.Collections.Generic;

namespace AnFake.Api
{
	/// <summary>
	///		Hyperlink related extensions.
	/// </summary>
	public static class HyperlinkExtension
	{
		/// <summary>
		///		Shortcut for <c>Links.Add(new Hyperlink(...))</c>.
		/// </summary>
		/// <param name="links"></param>
		/// <param name="href"></param>
		/// <param name="label"></param>
		public static void Add(this ICollection<Hyperlink> links, Uri href, string label)
		{
			links.Add(new Hyperlink(href, label));
		}

		/// <summary>
		///		Shortcut for <c>Links.Add(new Hyperlink(...))</c>.
		/// </summary>
		/// <param name="links"></param>
		/// <param name="href"></param>
		/// <param name="label"></param>
		public static void Add(this ICollection<Hyperlink> links, string href, string label)
		{
			links.Add(new Hyperlink(href, label));
		}
	}
}