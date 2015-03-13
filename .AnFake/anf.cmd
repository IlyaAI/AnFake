@echo off

if exist "%~dp0\AnFake.exe" (
	"%~dp0\AnFake.exe" %*
	goto eof
)

if exist "%~dp0\.AnFake\AnFake.exe" (
	"%~dp0\.AnFake\AnFake.exe" %*
	goto eof
)

echo "AnFake.exe not found."
exit -1

:eof