using AnFake.Api;
using AnFake.Integration.Tfs2012;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AnFake.Plugins.Tfs2012.Test
{
	[TestClass]
	public class TfsMessageBuilderTest
	{
		[TestCategory("Unit")]
		[TestMethod]
		public void EmbedLinks_should_embed_link()
		{
			// arrange
			var builder = new TfsMessageBuilder();
			builder.Append("Some Link");

			// act
			var msg = builder
				.EmbedLinks(new[] {new Hyperlink("about:blank", "Some Link")})
				.ToString();

			// assert
			Assert.AreEqual("[Some Link](about:blank)", msg);
		}

		[TestCategory("Unit")]
		[TestMethod]
		public void EmbedLinks_should_embed_link_label_double_quoted()
		{
			// arrange
			var builder = new TfsMessageBuilder();
			builder.Append("\"Some Link\"");

			// act
			var msg = builder
				.EmbedLinks(new[] { new Hyperlink("about:blank", "Some Link") })
				.ToString();

			// assert
			Assert.AreEqual("\"[Some Link](about:blank)\"", msg);
		}

		[TestCategory("Unit")]
		[TestMethod]
		public void EmbedLinks_should_embed_link_label_quoted()
		{
			// arrange
			var builder = new TfsMessageBuilder();
			builder.Append("'Some Link'");

			// act
			var msg = builder
				.EmbedLinks(new[] { new Hyperlink("about:blank", "Some Link") })
				.ToString();

			// assert
			Assert.AreEqual("'[Some Link](about:blank)'", msg);
		}

		[TestCategory("Unit")]
		[TestMethod]
		public void EmbedLinks_should_embed_link_label_in_square_brackets()
		{
			// arrange
			var builder = new TfsMessageBuilder();
			builder.Append("[Some Link]");

			// act
			var msg = builder
				.EmbedLinks(new[] { new Hyperlink("about:blank", "Some Link") })
				.ToString();

			// assert
			Assert.AreEqual("[[Some Link](about:blank)]", msg);
		}

		[TestCategory("Unit")]
		[TestMethod]
		public void EmbedLinks_should_embed_link_label_in_round_brackets()
		{
			// arrange
			var builder = new TfsMessageBuilder();
			builder.Append("(Some Link)");

			// act
			var msg = builder
				.EmbedLinks(new[] { new Hyperlink("about:blank", "Some Link") })
				.ToString();

			// assert
			Assert.AreEqual("([Some Link](about:blank))", msg);
		}

		[TestCategory("Unit")]
		[TestMethod]
		public void EmbedLinks_should_append_link_if_no_label_in_message()
		{
			// arrange
			var builder = new TfsMessageBuilder();
			builder.Append("Some Text");

			// act
			var msg = builder
				.EmbedLinks(new[] { new Hyperlink("about:blank", "Some Link") })
				.ToString();

			// assert
			Assert.AreEqual("Some Text [Some Link](about:blank)", msg);
		}

		[TestCategory("Unit")]
		[TestMethod]
		public void EmbedLinks_should_append_link_if_label_is_not_separated()
		{
			// arrange
			var builder = new TfsMessageBuilder();
			builder.Append("Some Link");

			// act
			var msg = builder
				.EmbedLinks(new[] { new Hyperlink("about:blank", "me Li") })
				.ToString();

			// assert
			Assert.AreEqual("Some Link [me Li](about:blank)", msg);
		}

		[TestCategory("Unit")]
		[TestMethod]
		public void EmbedLinks_should_do_nothing_if_already_embedded()
		{
			// arrange
			var builder = new TfsMessageBuilder();
			builder.Append("[Some Link](about:blank)");

			// act
			var msg = builder
				.EmbedLinks(new[] { new Hyperlink("about:blank", "Some Link") })
				.ToString();

			// assert
			Assert.AreEqual("[Some Link](about:blank)", msg);
		}
	}
}