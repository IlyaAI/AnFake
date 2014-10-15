using AnFake.Core;
using AnFake.Csx;

public sealed class BuildScript : BuildScriptSkeleton
{	
	public override void Run()
	{
		var dlpBin = "dlp";
		var officeFiles = Files.Match("office/*");

		Logger.Debug("Converting office files...");

		foreach (var file in officeFiles)
		{
			if (Files.Exists("txt/" + file.Name + ".txt"))
				continue;

			Logger.DebugFormat("  {0}", file.Name);

			Process.Run(p =>
			{
				p.FileName = dlpBin.slash("isys_doc2text.exe");
				p.Arguments = "--options EXCELMODE=CSV;PDFPHYSLAYOUT=True -s --output txt/" + file.Name + ".txt office/" + file.Name;
			});
		}
	}
}