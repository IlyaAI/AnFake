using AnFake.Core;

namespace AnFake.Plugins.HtmlSummary
{
	public static class HtmlSummary
	{
		public static void UseIt()
		{
			Plugin.Register<HtmlSummaryPlugin>();
		}
	}
}
