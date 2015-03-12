@echo off

if exist "$REL_PATH\AnFake.exe" (
	"$REL_PATH\AnFake.exe" %*
	goto eof
)

echo "AnFake.exe not found."
exit -1

:eof