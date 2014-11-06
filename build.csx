using AnFake.Core;
using AnFake.Csx;

public sealed class BuildScript : BuildScriptSkeleton
{	
	public override void Configure()
	{
		var solution = "AnFake.sln".AsFile();

		var tests = new [] 
			{
				"AnFake.Api.Test/bin/Debug/AnFake.Api.Test.dll",
				"AnFake.Core.Test/bin/Debug/AnFake.Core.Test.dll"
			}.AsFileSet();

		"Compile".AsTarget().Do(() =>
    		{
				MsBuild.Build(solution, p => 
 					{
						p.Properties["Configuration"] = "Debug";
						p.Properties["Platform"] = "Any CPU";
					});
			}
		);

		"Test.Unit".AsTarget().Do(() => 
			{
				MsTest.Run(tests, p => p.NoIsolation = true);
			}			
		);

		"Build".AsTarget().DependsOn("Compile", "Test.Unit");		
	}
}