using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.XPath;
using AnFake.Core.Exceptions;
using AnFake.Core.Utils;

namespace AnFake.Core
{
	public static class Xml
	{
		public sealed class XNode
		{
			private readonly XPathNavigator _navigator;
			private readonly XmlNamespaceManager _ns;

			internal XNode(XPathNavigator navigator)
			{
				_navigator = navigator;

				// ReSharper disable once AssignNullToNotNullAttribute
				_ns = new XmlNamespaceManager(_navigator.NameTable);
			}

			private XNode(XPathNavigator navigator, XmlNamespaceManager ns)
			{
				_navigator = navigator;
				_ns = ns;
			}

			public XNode Ns(string prefix, string uri)
			{
				_ns.AddNamespace(prefix, uri);
				return this;
			}

			public string Attr(string name)
			{
				return _navigator.GetAttribute(name, "");
			}

			public string Value()
			{
				return _navigator.Value;
			}

			public string ValueOf(string xpath, string defaultValue = "")
			{
				var subNode = _navigator.SelectSingleNode(xpath, _ns);
				return subNode == null
					? defaultValue
					: subNode.Value;
			}

			public IEnumerable<XNode> Select(string xpath)
			{
				return _navigator.Select(xpath, _ns)
					.AsEnumerable()
					.Select(x => new XNode(x, _ns));
			}

			public XNode SelectSingle(string xpath)
			{
				var subNode = _navigator.SelectSingleNode(xpath, _ns);
				if (subNode == null)
					throw new InvalidConfigurationException(String.Format("Node '{0}' not found in XML document.", xpath));

				return new XNode(subNode, _ns);
			}
		}

		public static XNode AsXmlDoc(this FileSystemPath xmlPath)
		{
			var xdoc = new XPathDocument(xmlPath.Full);

			return new XNode(xdoc.CreateNavigator());
		}

		public static XNode AsXmlDoc(this Stream stream)
		{
			var xdoc = new XPathDocument(stream);

			return new XNode(xdoc.CreateNavigator());
		}

		private static IEnumerable<XPathNavigator> AsEnumerable(this XPathNodeIterator nodes)
		{
			return new EnumerableAdapter<XPathNavigator>(nodes);
		}
	}
}