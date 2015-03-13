param($installPath, $toolsPath, $package)

$anfake = Join-Path $installPath bin\AnFake.exe
&($anfake) [AnFakeExtras]/nuget.fsx InitiateSolution