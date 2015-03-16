@echo off

if not exist "%~dp0\.AnFake" (
	"%~dp0\$REL_PATH\AnFake.exe" [AnFakeExtras]/nuget.fsx ValidateSolution
	if %ERRORLEVEL% neq 0 (
		exit %ERRORLEVEL%
	)
)

"%~dp0\.AnFake\AnFake.exe" %*