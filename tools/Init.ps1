param($installPath, $toolsPath, $package)

$anfake = Join-Path $installPath AnFake.exe
&($anfake) [AnFakeExtras]/nuget.fsx InitiateSolution