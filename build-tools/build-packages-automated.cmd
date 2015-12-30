@ECHO OFF

REM TeamCity chokes on the COMPONENTS_VERSION processing here, it thinks
REM COMPONENTS_VERSION is required going into the build step when we would
REM define it in the build step.

SETLOCAL

REM ---------------------------------------------------------------------------
REM Check usage
REM ---------------------------------------------------------------------------

IF "%1" == "" GOTO Usage


SET BUILD_NUMBER=%1

REM ---------------------------------------------------------------------------
REM Build packages
REM ---------------------------------------------------------------------------

SET /P COMPONENTS_VERSION=<ver.Aris.Proto.Native.txt

SET BUILD_VERSION=%COMPONENTS_VERSION%.%BUILD_NUMBER%

call build-packages.cmd %BUILD_VERSION%

GOTO :EOF

:Usage
ECHO USAGE: build-packages-automated.cmd (build-number)

EXIT 1

GOTO :EOF

ENDLOCAL
