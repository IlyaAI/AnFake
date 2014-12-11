using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AnFake.Core.Test
{
	[TestClass]
	public class TextDocTest
	{
		public static readonly string Eol = Environment.NewLine;

		[TestCategory("Unit")]
		[TestMethod]
		public void TextDoc_should_insert_before_first_line_in_empty_doc()
		{
			// arrange
			var doc = "".AsTextDoc();

			// act
			doc.FirstLine().InsertBefore("first");

			// assert
			Assert.AreEqual("first" + Eol, doc.Text);
		}

		[TestCategory("Unit")]
		[TestMethod]
		public void TextDoc_should_insert_after_last_line_in_empty_doc()
		{
			// arrange
			var doc = "".AsTextDoc();

			// act
			doc.LastLine().InsertAfter("last");

			// assert
			Assert.AreEqual(Eol + "last", doc.Text);
		}

		[TestCategory("Unit")]
		[TestMethod]
		public void TextDoc_should_replace_first_line_in_empty_doc()
		{
			// arrange
			var doc = "".AsTextDoc();

			// act
			doc.FirstLine().Replace("first");

			// assert
			Assert.AreEqual("first", doc.Text);
		}

		[TestCategory("Unit")]
		[TestMethod]
		public void TextDoc_should_replace_last_line_in_empty_doc()
		{
			// arrange
			var doc = "".AsTextDoc();

			// act
			doc.LastLine().Replace("last");

			// assert
			Assert.AreEqual("last", doc.Text);
		}

		[TestCategory("Unit")]
		[TestMethod]
		public void TextDoc_should_match_line_by_pattern()
		{
			// arrange
			var doc = "line 1\nline 2\nline 3".AsTextDoc();

			// act
			doc.MatchedLine("line 2").Replace("matched");

			// assert
			Assert.AreEqual("line 1" + Eol + "matched" + Eol + "line 3", doc.Text);
		}

		[TestCategory("Unit")]
		[TestMethod]
		public void TextDoc_should_match_multi_lines_by_pattern()
		{
			// arrange
			var doc = "line 1.1\nline 2\nline 1.2".AsTextDoc();

			// act
			doc.ForEachMatchedLine("line 1", x=> x.Replace("matched"));

			// assert
			Assert.AreEqual("matched" + Eol + "line 2" + Eol + "matched", doc.Text);
		}

		[TestCategory("Unit")]
		[TestMethod]
		public void TextDoc_should_insert_before_each_matched_line()
		{
			// arrange
			var doc = "line 1.1\nline 2\nline 1.2".AsTextDoc();

			// act
			doc.ForEachMatchedLine("line 1", x => x.InsertBefore("before"));

			// assert
			Assert.AreEqual("before" + Eol + "line 1.1" + Eol + "line 2" + Eol + "before" + Eol + "line 1.2", doc.Text);
		}

		[TestCategory("Unit")]
		[TestMethod]
		public void TextDoc_should_remove_matched_lines()
		{
			// arrange
			var doc = "line 1.1\nline 2\nline 1.2".AsTextDoc();

			// act
			doc.ForEachMatchedLine("line 1", x => x.Remove());

			// assert
			Assert.AreEqual("line 2", doc.Text);
		}

		[TestCategory("Unit")]
		[TestMethod]
		public void TextDoc_should_replace_in_line_by_pattern()
		{
			// arrange
			var doc = "before${var}after".AsTextDoc();

			// act
			doc.FirstLine().Replace("\\${var}", "inside");

			// assert
			Assert.AreEqual("beforeinsideafter", doc.Text);
		}

		[TestCategory("Unit")]
		[TestMethod]
		public void TextDoc_should_replace_in_line_by_groups()
		{
			// arrange
			var doc = "groupA-groupB groupC-groupD".AsTextDoc();

			// act
			doc.FirstLine().Replace("(group.)-(group.)", (i, x) => i > 0 ? x + i : null);

			// assert
			Assert.AreEqual("groupA1-groupB2 groupC1-groupD2", doc.Text);
		}

		[TestCategory("Unit")]
		[TestMethod]
		public void TextDoc_should_replace_text_by_pattern()
		{
			// arrange
			var doc = "before${var}after".AsTextDoc();

			// act
			doc.Replace("\\${var}", "inside");

			// assert
			Assert.AreEqual("beforeinsideafter", doc.Text);
		}

		[TestCategory("Unit")]
		[TestMethod]
		public void TextDoc_should_replace_text_by_groups()
		{
			// arrange
			var doc = "groupA-groupB groupC-groupD".AsTextDoc();

			// act
			doc.Replace("(group.)-(group.)", (i, x) => i > 0 ? x + i : null);

			// assert
			Assert.AreEqual("groupA1-groupB2 groupC1-groupD2", doc.Text);
		}
	}
}
