using AnFake.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AnFake.Plugins.Tfs2012.Test
{
	public abstract class TfsTestSuite
	{
		public Settings TfsSettings;

		public string TfsUri
		{
			get { return TfsSettings.Get("TfsUri"); }
		}

		public string TeamProject
		{
			get { return TfsSettings.Get("TeamProject"); }			
		}

		public string BuildDefinition
		{
			get { return TfsSettings.Get("BuildDefinition"); }
		}

		public string DropLocation
		{
			get { return TfsSettings.Get("DropLocation"); }
		}

		public virtual void Initialize()
		{
			TfsSettings = new Settings("[ApplicationData]/AnFake/tfs-test.json".AsPath());

			if (!TfsSettings.Has("TfsUri"))
				Assert.Inconclusive("There is no settings provided for integration test. Put appropriate values into '[ApplicationData]/AnFake/tfs-test.json' file.");
		}		
	}
}