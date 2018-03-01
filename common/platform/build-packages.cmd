@ECHO OFF
REM ---------------------------------------------------------------------------
REM Builds the file utility nuget packages.
REM ---------------------------------------------------------------------------

SETLOCAL

SET BUILD_NUMBER=%1
IF "%BUILD_NUMBER%" == "" SET BUILD_NUMBER=5555
ECHO BUILD_NUMBER=%BUILD_NUMBER%

SET /p VERSION=<ver.platform.txt

ECHO Making packages for %VERSION%.%BUILD_NUMBER%

SET PACKCMD=dotnet pack -c Release /p:Version=%VERSION%
ECHO PACKCMD=%PACKCMD%

%PACKCMD% .\SoundMetrics.Aris.Messages\

ENDLOCAL
