namespace AnFake.Plugins.Tfs2012.Test
{
	public abstract class TfsTestSuite
	{
		public const string TfsUri = "https://nsk-tfs.avp.ru:8081/tfs/dlpr";
		public const string TeamProject = "DLP_PDK";
		public const string BuildDefinition = "FAKE-test";
		public const string DropLocation = @"\\nsk-fs\Inbox\Ivanov Ilya";
	}
}