param($installPath, $toolsPath, $package)

Write-Host "<<< AnFake: Another F# Make >>>"

$anfake = Join-Path $installPath bin\AnFake.exe
&($anfake) [AnFakeExtras]/nuget.fsx InitiateSolution -p "$installPath" | Write-Host

IF ($LASTEXITCODE -NE 0) 
{
	Write-Host "There are some errors in initiating solution. See output above." -ForegroundColor White -BackgroundColor Red
} 
ELSE
{ 
	Write-Host "Use '.\anf ""[AnFakeExtras]/vs-setup.fsx"" Tools -p <local-projects-home>' to setup AnFake tools into Visual Studio." -ForegroundColor White -BackgroundColor DarkGreen
	Write-Host "Use '.\anf ""[AnFakeExtras]/vs-setup.fsx"" BuildTemplate -p <team-project-name>' to setup AnFake build process template into Team Build." -ForegroundColor White -BackgroundColor DarkGreen	
	Write-Host "Use '.\anf Build' to build your solution with AnFake." -ForegroundColor White -BackgroundColor DarkGreen
}