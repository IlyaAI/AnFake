using System;
using System.Collections.Generic;
using System.Xml;
using System.Xml.XPath;
using AnFake.Core.Utils;

namespace AnFake.Core.Xml
{
	public static class XmlExtension
	{
		public static IEnumerable<XPathNavigator> AsEnumerable(this XPathNodeIterator nodes)
		{
			return new EnumerableAdapter<XPathNavigator>(nodes);
		}

		public static string Attr(this XPathNavigator node, string name)
		{
			return node.GetAttribute(name, "");
		}

		public static string Value(this XPathNavigator node)
		{
			return node.Value;
		}

		public static string Value(this XPathNavigator node, string path, IXmlNamespaceResolver ns)
		{
			var subNode = node.SelectSingleNode(path, ns);
			return subNode == null
				? String.Empty
				: subNode.Value;
		}
	}
}