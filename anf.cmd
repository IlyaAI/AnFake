@echo off

if exist "%~dp0\AnFake.exe" (
	"%~dp0\AnFake.exe" %1 %2 %3 %4 %5 %6 %7 %7 %8 %9	
	goto eof
)

if exist "%~dp0\.AnFake\AnFake.exe" (
	"%~dp0\.AnFake\AnFake.exe" %1 %2 %3 %4 %5 %6 %7 %7 %8 %9
	goto eof
)

echo "AnFake.exe not found."
exit -1

:eof