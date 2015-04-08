using System;
using AnFake.Integration.Tfs2012;
using Microsoft.TeamFoundation.VersionControl.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AnFake.Plugins.Tfs2012.Test
{
	[TestClass]
	public class VcsMappingsTest
	{
		[TestCategory("Unit")]
		[TestMethod]
		public void VcsMappings_should_provide_default_mapping()
		{
			// arrange		

			// act
			var mappings = VcsMappings.Parse("", "$/server/root", @"C:\LocalPath");

			// assert
			Assert.AreEqual(1, mappings.Length);

			Assert.AreEqual("$/server/root", mappings[0].ServerItem);
			Assert.AreEqual(@"C:\LocalPath", mappings[0].LocalItem);
			Assert.IsFalse(mappings[0].IsCloaked);
		}

		[TestCategory("Unit")]
		[TestMethod]
		public void VcsMappings_should_map_relative_server_pathes()
		{
			// arrange
			const string workspace = "/server/path/01: local\\path\\01\nserver/path/02: local\\path\\02";

			// act
			var mappings = VcsMappings.Parse(workspace, "$/server/root", @"c:\local\root");

			// assert
			Assert.AreEqual(3, mappings.Length);

			Assert.AreEqual("$/server/root/server/path/01", mappings[1].ServerItem);
			Assert.AreEqual(@"c:\local\root\local\path\01", mappings[1].LocalItem);
			Assert.IsFalse(mappings[1].IsCloaked);

			Assert.AreEqual("$/server/root/server/path/02", mappings[2].ServerItem);
			Assert.AreEqual(@"c:\local\root\local\path\02", mappings[2].LocalItem);
			Assert.IsFalse(mappings[2].IsCloaked);
		}

		[TestCategory("Unit")]
		[TestMethod]
		public void VcsMappings_should_map_absolute_server_path()
		{
			// arrange
			const string workspace = "$/server/path/01: local\\path\\01";

			// act
			var mappings = VcsMappings.Parse(workspace, "$/server/root", @"c:\local\root");

			// assert
			Assert.AreEqual(2, mappings.Length);

			Assert.AreEqual("$/server/path/01", mappings[1].ServerItem);
			Assert.AreEqual(@"c:\local\root\local\path\01", mappings[1].LocalItem);
			Assert.IsFalse(mappings[1].IsCloaked);
		}

		[TestCategory("Unit")]
		[TestMethod]
		public void VcsMappings_should_cloak_relative_server_pathes()
		{
			// arrange
			const string workspace = "-/server/path/01\n-server/path/02";

			// act
			var mappings = VcsMappings.Parse(workspace, "$/server/root", @"c:\local\root");

			// assert
			Assert.AreEqual(3, mappings.Length);

			Assert.AreEqual("$/server/root/server/path/01", mappings[1].ServerItem);
			Assert.IsTrue(mappings[1].IsCloaked);

			Assert.AreEqual("$/server/root/server/path/02", mappings[2].ServerItem);
			Assert.IsTrue(mappings[2].IsCloaked);
		}

		[TestCategory("Unit")]
		[TestMethod]
		public void VcsMappings_should_cloak_absolute_server_path()
		{
			// arrange
			const string workspace = "-$/server/path/01";

			// act
			var mappings = VcsMappings.Parse(workspace, "$/server/root", @"c:\local\root");

			// assert
			Assert.AreEqual(2, mappings.Length);

			Assert.AreEqual("$/server/path/01", mappings[1].ServerItem);
			Assert.IsTrue(mappings[1].IsCloaked);
		}

		[TestCategory("Unit")]
		[TestMethod]
		public void VcsMappings_should_trim_spaces()
		{
			// arrange
			const string workspace = " $/server/path/01 : local\\path\\01 \n" +
									" server/path/02 : local\\path\\02 \n" +
									" /server/path/03 : local\\path\\03 \n" +
									" - $/server/path/04 : \n" +
									" - server/path/05 : \n" +
									" - /server/path/06 : \n" +
									" - $/server/path/07 \n";

			// act
			var mappings = VcsMappings.Parse(workspace, "$/server/root", @"c:\local\root");

			// assert
			Assert.AreEqual(8, mappings.Length);

			Assert.AreEqual("$/server/path/01", mappings[1].ServerItem);
			Assert.AreEqual(@"c:\local\root\local\path\01", mappings[1].LocalItem);
			Assert.IsFalse(mappings[1].IsCloaked);

			Assert.AreEqual("$/server/root/server/path/02", mappings[2].ServerItem);
			Assert.AreEqual(@"c:\local\root\local\path\02", mappings[2].LocalItem);
			Assert.IsFalse(mappings[2].IsCloaked);

			Assert.AreEqual("$/server/root/server/path/03", mappings[3].ServerItem);
			Assert.AreEqual(@"c:\local\root\local\path\03", mappings[3].LocalItem);
			Assert.IsFalse(mappings[3].IsCloaked);

			Assert.AreEqual("$/server/path/04", mappings[4].ServerItem);
			Assert.IsTrue(mappings[4].IsCloaked);

			Assert.AreEqual("$/server/root/server/path/05", mappings[5].ServerItem);
			Assert.IsTrue(mappings[5].IsCloaked);

			Assert.AreEqual("$/server/root/server/path/06", mappings[6].ServerItem);
			Assert.IsTrue(mappings[6].IsCloaked);

			Assert.AreEqual("$/server/path/07", mappings[7].ServerItem);
			Assert.IsTrue(mappings[7].IsCloaked);
		}

		[TestCategory("Unit")]
		[ExpectedException(typeof (FormatException))]
		[TestMethod]
		public void VcsMappings_should_throw_if_no_semicolon()
		{
			// arrange
			const string workspace = "$/server/path/01 local//path";

			// act
			VcsMappings.Parse(workspace, "$/", @"c:\local\root");

			// assert			
		}

		[TestCategory("Unit")]
		[ExpectedException(typeof (FormatException))]
		[TestMethod]
		public void VcsMappings_should_throw_if_multi_semicolons()
		{
			// arrange
			const string workspace = "$/server/path/01: c:\\local\\root";

			// act
			VcsMappings.Parse(workspace, "$/", @"c:\another\root");

			// assert			
		}

		[TestCategory("Unit")]
		[ExpectedException(typeof (FormatException))]
		[TestMethod]
		public void VcsMappings_should_throw_if_local_path_rooted()
		{
			// arrange
			const string workspace = "$/server/path/01: \\local\\root";

			// act
			VcsMappings.Parse(workspace, "$/", @"c:\another\root");

			// assert			
		}

		[TestCategory("Unit")]
		[TestMethod]
		public void VcsMappings_should_parse_versioned_by_changeset_path()
		{
			// arrange
			const string workspace = " $/server/path/01;C100: 01";

			// act
			var mappings = VcsMappings.Parse(workspace, "$/server/root", @"c:\");

			// assert
			Assert.AreEqual(2, mappings.Length);
			Assert.AreEqual("$/server/path/01", mappings[1].ServerItem);
			Assert.AreEqual(new ChangesetVersionSpec(100), mappings[1].VersionSpec);			
		}

		[TestCategory("Unit")]
		[TestMethod]
		public void VcsMappings_should_parse_versioned_as_latest_path()
		{
			// arrange
			const string workspace = " $/server/path/01;T: 01";

			// act
			var mappings = VcsMappings.Parse(workspace, "$/server/root", @"c:\");

			// assert
			Assert.AreEqual(2, mappings.Length);
			Assert.AreEqual("$/server/path/01", mappings[1].ServerItem);
			Assert.AreEqual(VersionSpec.Latest, mappings[1].VersionSpec);
		}
	}
}