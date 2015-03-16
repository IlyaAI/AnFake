param($installPath, $toolsPath, $package)

Write-Host "<<< AnFake: Another F# Make >>>"

$anfake = Join-Path $installPath bin\AnFake.exe
&($anfake) [AnFakeExtras]/nuget.fsx InitiateSolution ValidateSolution | Write-Host
