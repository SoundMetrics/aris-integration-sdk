@ECHO OFF

REM Invokes an F# script. The script file path must be the first argument.
REM (Knows where to find things to invoke it.)

SETLOCAL

REM For the very silly build server which stumbles over %whatever% when it doesn't
REM know what it is:
REM Put F# path first so it overrides other installations.
set FSHARP_PATH=%ProgramFiles(x86)%\Microsoft SDKs\F#\10.1\Framework\v4.0
set PATH=%FSHARP_PATH%;%PATH%

fsi %*
exit /b %ERRORLEVEL%

ENDLOCAL
